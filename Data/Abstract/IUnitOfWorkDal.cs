using Microsoft.EntityFrameworkCore.Storage;

namespace Data.Abstract
{
    public interface IUnitOfWorkDal : IDisposable
    {
        IGenericDal<T> Repository<T>() where T : class;
        Task<int> SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
