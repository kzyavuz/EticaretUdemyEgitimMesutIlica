using System.Linq.Expressions;

namespace Service.Service
{
    public interface IGenericService<T> where T : class
    {
        IQueryable<T> Queryable();

        Task<List<T>> GetListAsync(
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, object>>? orderBy = null,
            bool descending = false,
            int? take = null,
            params Expression<Func<T, object>>[] includes
        );

        Task<T> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes);

        Task<bool> InsertRangeAsync(IEnumerable<T> entities);

        Task<bool> CreateAsync(T entity);

        Task<bool> UpdateRangeAsync(IEnumerable<T> entities);
        Task<bool> UpdateAsync(T entity);

        Task<bool> DeleteAsync(int id);

        Task<bool> RemoveAsync(int id);

        Task UpdateFeaturedStatusAsync(List<int> visibleIds, List<int> selectedIds);

        Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);
        Task<T?> GetFirstAsync(
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, object>>? orderBy = null,
            bool descending = false,
            params Expression<Func<T, object>>[] includes
        );
    }
}
