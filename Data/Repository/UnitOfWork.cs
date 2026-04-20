using Core.Abstract;
using Data.Abstract;
using Data.Context;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Data.Repository
{
    public class UnitOfWork : IUnitOfWorkDal, IDisposable
    {
        private readonly DatabaseContext _context;
        private readonly IQueryContext _queryContext;
        private readonly ILoggerFactory _loggerFactory; // Logger oluşturmak için eklendi
        private readonly Dictionary<string, object> _repositories;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(DatabaseContext context, IQueryContext queryContext, ILoggerFactory loggerFactory)
        {
            _context = context;
            _queryContext = queryContext;
            _loggerFactory = loggerFactory;
            _repositories = new Dictionary<string, object>();
        }

        public IGenericDal<T> Repository<T>() where T : class
        {
            var typeName = typeof(T).Name;

            if (!_repositories.ContainsKey(typeName))
            {
                // GenericRepository'nin beklediği Logger'ı burada oluşturup veriyoruz
                var repositoryInstance = new GenericRepository<T>(_context, _queryContext, _loggerFactory.CreateLogger<GenericRepository<T>>());
                _repositories.Add(typeName, repositoryInstance);
            }

            return (IGenericDal<T>)_repositories[typeName];
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("A transaction is already in progress.");
            }

            _transaction = await _context.Database.BeginTransactionAsync();
            return _transaction;
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction to commit.");
            }

            try
            {
                await SaveChangesAsync();
                await _transaction.CommitAsync();
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction to rollback.");
            }

            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();
                    _context.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
