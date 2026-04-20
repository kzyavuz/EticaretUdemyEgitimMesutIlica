using Core.Abstract;
using Core.Entities;
using Data.Abstract;
using Data.Context;
using Data.Repository;
using Microsoft.Extensions.Logging;

namespace Data.EntityFramework
{
    public class EFAdressDal : GenericRepository<Adress>, IAdressDal
    {
        public EFAdressDal(DatabaseContext context, IQueryContext queryContext, ILogger<GenericRepository<Adress>> logger) : base(context, queryContext, logger)
        {
        }
    }
}
