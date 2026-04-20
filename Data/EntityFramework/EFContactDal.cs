using Core.Abstract;
using Core.Entities;
using Data.Abstract;
using Data.Context;
using Data.Repository;
using Microsoft.Extensions.Logging;

namespace Data.EntityFramework
{
    public class EFContactDal : GenericRepository<Contact>, IContactDal
    {
        public EFContactDal(DatabaseContext context, IQueryContext queryContext, ILogger<GenericRepository<Contact>> logger) : base(context, queryContext, logger)
        {
        }
    }
}
