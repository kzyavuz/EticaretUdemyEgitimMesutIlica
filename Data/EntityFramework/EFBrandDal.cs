using Core.Abstract;
using Core.Entities;
using Data.Abstract;
using Data.Context;
using Data.Repository;
using Microsoft.Extensions.Logging;

namespace Data.EntityFramework
{
    public class EFBrandDal : GenericRepository<Brand>, IBrandDal
    {
        public EFBrandDal(DatabaseContext context, IQueryContext queryContext, ILogger<GenericRepository<Brand>> logger) : base(context, queryContext, logger)
        {
        }
    }
}
