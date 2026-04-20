using Core.Dto;
using System.Linq.Expressions;

namespace Data.Abstract
{
    public interface IGenericDal<T> where T : class
    {
        IQueryable<T> TGetQueryable();

        Task<List<T>> TGetListAsync(
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, object>>? orderBy = null,
            bool descending = false,
            int? take = null,
            params Expression<Func<T, object>>[] includes
        );

        Task<T?> TGetByIdAsync(int id, params Expression<Func<T, object>>[] includes);

        Task<bool> TInsertRangeAsync(IEnumerable<T> entities);

        Task<bool> TInsertAsync(T entity);

        Task<bool> TUpdateRangeAsync(IEnumerable<T> entities);

        Task<bool> TUpdateAsync(T entity);

        Task<bool> TDeleteAsync(T entity);

        Task<bool> TRemoveAsync(T entity);

        Task TUpdateFeaturedStatusAsync(List<int> visibleIds, List<int> selectedIds);

        Task<int> TCountAsync(Expression<Func<T, bool>>? filter = null);

        Task<T?> TGetFirstAsync(
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, object>>? orderBy = null,
            bool descending = false,
            params Expression<Func<T, object>>[] includes
        );

        Task<PagedResult<T>> TGetPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, object>>? orderBy = null,
            bool descending = false,
            params Expression<Func<T, object>>[] includes
        );
    }
}
