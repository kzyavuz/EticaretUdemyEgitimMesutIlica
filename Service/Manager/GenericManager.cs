using Data.Abstract;
using Service.Service;
using System.Linq.Expressions;

namespace Service.Manager
{
    public class GenericManager<T> : IGenericService<T> where T : class
    {
        private readonly IUnitOfWorkDal _unitOfWork;
        private readonly Lazy<IGenericDal<T>> _dal;

        public GenericManager(IUnitOfWorkDal unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _dal = new Lazy<IGenericDal<T>>(() => _unitOfWork.Repository<T>());
        }

        private IGenericDal<T> Dal => _dal.Value;

        public IQueryable<T> Queryable()
        {
            return Dal.TGetQueryable();
        }

        public async Task<List<T>> GetListAsync(
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, object>>? orderBy = null,
            bool descending = false,
            int? take = null,
            params Expression<Func<T, object>>[] includes
        )
        {
            return await Dal.TGetListAsync(filter, orderBy, descending, take, includes);
        }

        public async Task<T> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes)
        {
            var entity = await Dal.TGetByIdAsync(id, includes);
            if (entity == null)
                throw new KeyNotFoundException("Kayıt bulunamadı.");
            return entity;
        }

        public async Task<T?> GetFirstAsync(
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, object>>? orderBy = null,
            bool descending = false,
            params Expression<Func<T, object>>[] includes)
        {
            return await Dal.TGetFirstAsync(filter, orderBy, descending, includes);
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
        {
            return await Dal.TCountAsync(filter);
        }

        public async Task<bool> CreateAsync(T entity)
        {
            var repoResult = await Dal.TInsertAsync(entity);
            if (!repoResult) return false;

            var saveResult = await _unitOfWork.SaveChangesAsync();
            return saveResult > 0;
        }

        public async Task<bool> UpdateAsync(T entity)
        {
            var repoResult = await Dal.TUpdateAsync(entity);
            if (!repoResult) return false;

            var saveResult = await _unitOfWork.SaveChangesAsync();
            return saveResult > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await Dal.TGetByIdAsync(id);
            if (entity == null) return false;

            var repoResult = await Dal.TDeleteAsync(entity);
            if (!repoResult) return false;

            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<bool> RemoveAsync(int id)
        {
            var entity = await Dal.TGetByIdAsync(id);
            if (entity == null) return false;

            var repoResult = await Dal.TRemoveAsync(entity);
            if (!repoResult) return false;

            return await _unitOfWork.SaveChangesAsync() > 0;
        }
        public async Task<bool> InsertRangeAsync(IEnumerable<T> entities)
        {
            var repoResult = await Dal.TInsertRangeAsync(entities);
            if (!repoResult) return false;

            var saveResult = await _unitOfWork.SaveChangesAsync();
            return saveResult > 0;
        }

        public async Task<bool> UpdateRangeAsync(IEnumerable<T> entities)
        {
            var repoResult = await Dal.TUpdateRangeAsync(entities);
            if (!repoResult) return false;

            var saveResult = await _unitOfWork.SaveChangesAsync();
            return saveResult > 0;
        }

        public async Task UpdateFeaturedStatusAsync(List<int> visibleIds, List<int> selectedIds)
        {
            await Dal.TUpdateFeaturedStatusAsync(visibleIds, selectedIds);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
