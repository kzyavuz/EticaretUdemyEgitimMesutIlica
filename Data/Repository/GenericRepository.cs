using Core.Abstract;
using Core.Dto;
using Core.Enum;
using Data.Abstract;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Data.Repository
{
    public class GenericRepository<T> : IGenericDal<T> where T : class
    {
        protected readonly DatabaseContext _context;
        protected readonly IQueryContext _queryContext;
        private readonly ILogger<GenericRepository<T>> _logger; // T parametresi eklendi

        public GenericRepository(DatabaseContext context, IQueryContext queryContext, ILogger<GenericRepository<T>> logger)
        {
            _context = context;
            _queryContext = queryContext;
            _logger = logger;
        }

        #region Read Operations (Okuma İşlemleri)

        private IQueryable<T> ApplyBaseFilters(IQueryable<T> query)
        {
            if (!typeof(IBaseEntity).IsAssignableFrom(typeof(T)))
                return query;

            // Not: EF.Property kullanımı "Shadow Property" veya Interface üzerindeki alanlara erişmek için doğrudur.
            return _queryContext.Mode switch
            {
                QueryMode.Admin => query.Where(x => EF.Property<DateTime?>(x, "DeletedDate") == null),
                QueryMode.Public => query.Where(x => EF.Property<DataStatus>(x, "Status") == DataStatus.Active),
                _ => query
            };
        }

        public IQueryable<T> TGetQueryable() => ApplyBaseFilters(_context.Set<T>().AsNoTracking());

        public async Task<List<T>> TGetListAsync(
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, object>>? orderBy = null,
            bool descending = false,
            int? take = null,
            params Expression<Func<T, object>>[] includes)
        {
            var query = TGetQueryable();

            if (filter != null) query = query.Where(filter);

            if (includes != null)
                foreach (var include in includes) query = query.Include(include);

            if (orderBy != null)
                query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

            if (take.HasValue) query = query.Take(take.Value);

            return await query.ToListAsync();
        }

        public async Task<T?> TGetByIdAsync(int id, params Expression<Func<T, object>>[] includes)
        {
            var query = TGetQueryable();
            if (includes != null)
                foreach (var include in includes) query = query.Include(include);

            return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
        }

        public async Task<T?> TGetFirstAsync(
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, object>>? orderBy = null,
            bool descending = false,
            params Expression<Func<T, object>>[] includes)
        {
            var query = TGetQueryable();
            if (filter != null) query = query.Where(filter);
            if (includes != null)
                foreach (var include in includes) query = query.Include(include);

            if (orderBy != null)
                query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

            return await query.FirstOrDefaultAsync();
        }

        public async Task<int> TCountAsync(Expression<Func<T, bool>>? filter = null)
        {
            var query = ApplyBaseFilters(_context.Set<T>());
            if (filter != null) query = query.Where(filter);
            return await query.AsNoTracking().CountAsync();
        }

        #endregion

        #region Write Operations (Yazma İşlemleri - Bool Dönüşlü)

        public async Task<bool> TInsertAsync(T entity)
        {
            try
            {
                if (entity is IBaseEntity baseEntity)
                {
                    baseEntity.UpdatedDate = DateTime.Now;
                }

                await _context.Set<T>().AddAsync(entity);
                return true;
            }
            catch (Exception ex)
            {
                LogError("Ekleme", ex);
                return false;
            }
        }

        public async Task<bool> TInsertRangeAsync(IEnumerable<T> entities)
        {
            try
            {
                foreach (var entity in entities)
                {
                    if (entity is IBaseEntity baseEntity)
                        baseEntity.UpdatedDate = DateTime.Now;
                }

                await _context.Set<T>().AddRangeAsync(entities);
                return true;
            }
            catch (Exception ex)
            {
                LogError("Toplu Ekleme", ex);
                return false;
            }
        }

        public async Task<bool> TUpdateAsync(T entity)
        {
            try
            {
                if (entity is IBaseEntity baseEntity)
                    baseEntity.UpdatedDate = DateTime.Now;

                _context.Set<T>().Update(entity);
                return true;
            }
            catch (Exception ex)
            {
                LogError("Güncelleme", ex);
                return false;
            }
        }

        public async Task<bool> TUpdateRangeAsync(IEnumerable<T> entities)
        {
            try
            {
                foreach (var entity in entities)
                {
                    if (entity is IBaseEntity baseEntity)
                        baseEntity.UpdatedDate = DateTime.Now;
                }

                _context.Set<T>().UpdateRange(entities);
                return true;
            }
            catch (Exception ex)
            {
                LogError("Toplu Güncelleme", ex);
                return false;
            }
        }

        /// <summary>
        /// Soft Delete (Silindi İşaretleme)
        /// </summary>
        public async Task<bool> TDeleteAsync(T entity)
        {
            try
            {
                if (entity is IBaseEntity baseEntity)
                {
                    baseEntity.DeletedDate = DateTime.Now;
                    // Eğer varsa statusu da pasife çekebilirsiniz:
                    // baseEntity.Status = DataStatus.Passive; 
                }

                _context.Set<T>().Update(entity);
                return true;
            }
            catch (Exception ex)
            {
                LogError("Soft Delete", ex);
                return false;
            }
        }

        /// <summary>
        /// Hard Delete (Veritabanından Tamamen Kaldırma)
        /// </summary>
        public async Task<bool> TRemoveAsync(T entity)
        {
            try
            {
                _context.Set<T>().Remove(entity);
                return true;
            }
            catch (Exception ex)
            {
                LogError("Hard Delete", ex);
                return false;
            }
        }


        public async Task TUpdateFeaturedStatusAsync(List<int> visibleIds, List<int> selectedIds)
        {
            if (!typeof(IFeatureable).IsAssignableFrom(typeof(T)))
                throw new InvalidOperationException("Bu entity öne çıkarma özelliğini desteklemiyor.");

            var entities = await TGetListAsync(filter: x => visibleIds.Contains(EF.Property<int>(x, "Id")));

            foreach (var entity in entities)
            {
                if (entity is not IFeatureable featureable)
                    continue;

                var idValue = (int)typeof(T).GetProperty("Id")!.GetValue(entity)!;
                var shouldBeFeatured = selectedIds.Contains(idValue);

                if (featureable.IsFeatured != shouldBeFeatured)
                {
                    featureable.IsFeatured = shouldBeFeatured;
                    await TUpdateAsync(entity);
                }
            }
        }

        #endregion

        #region Helper Methods (Yardımcı Metotlar)

        private void LogError(string operation, Exception ex)
        {
            _logger.LogError(ex, "{EntityName} üzerinde {Operation} işlemi sırasında beklenmedik bir hata oluştu.", typeof(T).Name, operation);
        }

        #endregion

        #region Other Methods

        public async Task<PagedResult<T>> TGetPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, object>>? orderBy = null,
            bool descending = false,
            params Expression<Func<T, object>>[] includes)
        {
            var query = TGetQueryable();
            if (filter != null) query = query.Where(filter);
            if (includes != null)
                foreach (var include in includes) query = query.Include(include);

            var totalCount = await query.CountAsync();

            if (orderBy != null)
                query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<T>(items, totalCount, pageNumber, pageSize);
        }
        #endregion
    }
}