//using Core.Abstract;
//using Core.Dto;
//using Core.Enum;
//using Data.Abstract;
//using Data.Context;
//using Microsoft.EntityFrameworkCore;
//using System.Linq.Expressions;

//namespace Data.Repository
//{
//    public class GenericRepository<T> : IGenericDal<T> where T : class
//    {
//        protected readonly DatabaseContext _context;
//        protected readonly IQueryContext _queryContext;

//        public GenericRepository(DatabaseContext context, IQueryContext queryContext)
//        {
//            _context = context;
//            _queryContext = queryContext;
//        }

//        private IQueryable<T> ApplyBaseFilters(IQueryable<T> query)
//        {
//            if (!typeof(IBaseEntity).IsAssignableFrom(typeof(T)))
//                return query;

//            return _queryContext.Mode switch
//            {
//                QueryMode.Admin => query.Where(x =>
//                    EF.Property<DateTime?>(x, "DeletedDate") != null),

//                QueryMode.Public => query.Where(x =>
//                    EF.Property<DataStatus>(x, "Status") == DataStatus.Active),

//                _ => query
//            };
//        }
//        public IQueryable<T> TGetQueryable()
//        {
//            // Veritabanı setini al ve temel filtreleri (Silinmiş mi? Aktif mi?) uygulayıp dön
//            return ApplyBaseFilters(_context.Set<T>().AsQueryable());
//        }

//        public async Task<List<T>> TGetListAsync(
//            Expression<Func<T, bool>>? filter = null,
//            Expression<Func<T, object>>? orderBy = null,
//            bool descending = false,
//            int? take = null,
//            params Expression<Func<T, object>>[] includes
//        )
//        {
//            var query = ApplyBaseFilters(_context.Set<T>().AsQueryable());

//            if (filter != null)
//                query = query.Where(filter);

//            if (includes != null)
//                foreach (var include in includes)
//                    query = query.Include(include);

//            if (orderBy != null)
//                query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

//            if (take.HasValue)
//                query = query.Take(take.Value);

//            return await query.AsNoTracking().ToListAsync();
//        }

//        public async Task<T?> TGetByIdAsync(int id, params Expression<Func<T, object>>[] includes)
//        {
//            var query = ApplyBaseFilters(_context.Set<T>().AsQueryable());

//            if (includes != null)
//                foreach (var include in includes)
//                    query = query.Include(include);

//            return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
//        }

//        public async Task TInsertRangeAsync(IEnumerable<T> entities)
//        {
//            foreach (var entity in entities)
//            {
//                if (entity is IBaseEntity baseEntity)
//                {
//                    baseEntity.UpdatedDate = DateTime.Now;
//                }
//            }

//            await _context.Set<T>().AddRangeAsync(entities);
//        }

//        public async Task TInsertAsync(T entity)
//        {
//            if (entity is IBaseEntity baseEntity)
//            {
//                baseEntity.UpdatedDate = DateTime.Now;
//            }

//            await _context.Set<T>().AddAsync(entity);
//        }

//        public virtual Task TUpdateRangeAsync(IEnumerable<T> entities)
//        {
//            foreach (var entity in entities)
//            {
//                if (entity is IBaseEntity baseEntity)
//                {
//                    baseEntity.UpdatedDate = DateTime.Now;
//                }
//            }

//            _context.Set<T>().UpdateRange(entities);
//            return Task.CompletedTask;
//        }

//        public virtual Task TUpdateAsync(T entity)
//        {
//            if (entity is IBaseEntity baseEntity)
//            {
//                baseEntity.UpdatedDate = DateTime.Now;
//            }

//            _context.Set<T>().Update(entity);
//            return Task.CompletedTask;
//        }

//        public async Task TDeleteAsync(T entity)
//        {
//            if (entity is IBaseEntity baseEntity)
//            {
//                baseEntity.DeletedDate = DateTime.Now;
//            }

//            _context.Set<T>().Update(entity);
//        }

//        public Task TRemoveAsync(T entity)
//        {
//            _context.Set<T>().Remove(entity);
//            return Task.CompletedTask;
//        }

//        public async Task<int> TCountAsync(Expression<Func<T, bool>>? filter = null)
//        {
//            var query = ApplyBaseFilters(_context.Set<T>());

//            if (filter != null)
//                query = query.Where(filter);

//            return await query.AsNoTracking().CountAsync();
//        }

//        public async Task<T?> TGetFirstAsync(
//            Expression<Func<T, bool>>? filter = null,
//            Expression<Func<T, object>>? orderBy = null,
//            bool descending = false,
//            params Expression<Func<T, object>>[] includes)
//        {
//            var query = ApplyBaseFilters(_context.Set<T>().AsQueryable());

//            if (filter != null)
//                query = query.Where(filter);

//            if (includes != null)
//                foreach (var include in includes)
//                    query = query.Include(include);

//            if (orderBy != null)
//                query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

//            return await query.FirstOrDefaultAsync();
//        }

//        public async Task<PagedResult<T>> TGetPagedAsync(
//            int pageNumber,
//            int pageSize,
//            Expression<Func<T, bool>>? filter = null,
//            Expression<Func<T, object>>? orderBy = null,
//            bool descending = false,
//            params Expression<Func<T, object>>[] includes)
//        {
//            var query = ApplyBaseFilters(_context.Set<T>().AsQueryable());

//            if (filter != null)
//                query = query.Where(filter);

//            if (includes != null)
//                foreach (var include in includes)
//                    query = query.Include(include);

//            var totalCount = await query.CountAsync();

//            if (orderBy != null)
//                query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

//            var items = await query
//                .Skip((pageNumber - 1) * pageSize)
//                .Take(pageSize)
//                .ToListAsync();

//            return new PagedResult<T>(items, totalCount, pageNumber, pageSize);
//        }

//        public async Task TUpdateFeaturedStatusAsync(List<int> visibleIds, List<int> selectedIds)
//        {
//            if (!typeof(IFeatureable).IsAssignableFrom(typeof(T)))
//                throw new InvalidOperationException("Bu entity öne çıkarma özelliğini desteklemiyor.");

//            var entities = await TGetListAsync(filter: x => visibleIds.Contains(EF.Property<int>(x, "Id")));

//            foreach (var entity in entities)
//            {
//                if (entity is not IFeatureable featureable)
//                    continue;

//                var idValue = (int)typeof(T).GetProperty("Id")!.GetValue(entity)!;
//                var shouldBeFeatured = selectedIds.Contains(idValue);

//                if (featureable.IsFeatured != shouldBeFeatured)
//                {
//                    featureable.IsFeatured = shouldBeFeatured;
//                    await TUpdateAsync(entity);
//                }
//            }
//        }
//    }
//}
