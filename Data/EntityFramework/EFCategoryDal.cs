using Core.Abstract;
using Core.Entities;
using Data.Abstract;
using Data.Context;
using Data.Repository;
using Microsoft.Extensions.Logging;

namespace Data.EntityFramework
{
    public class EFCategoryDal : GenericRepository<Category>, ICategoryDal
    {
        public EFCategoryDal(DatabaseContext context, IQueryContext queryContext, ILogger<GenericRepository<Category>> logger) : base(context, queryContext, logger)
        {
        }
    }
}
