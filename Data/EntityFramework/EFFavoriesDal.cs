using Core.Abstract;
using Core.Entities;
using Data.Abstract;
using Data.Context;
using Data.Repository;
using Microsoft.Extensions.Logging;

namespace Data.EntityFramework
{
    public class EFFavoriesDal : GenericRepository<Favories>, IFavoriesDal
    {
        public EFFavoriesDal(DatabaseContext context, IQueryContext queryContext, ILogger<GenericRepository<Favories>> logger) : base(context, queryContext, logger)
        {
        }
    }
}
