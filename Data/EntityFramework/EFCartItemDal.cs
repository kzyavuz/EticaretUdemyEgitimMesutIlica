using Core.Abstract;
using Core.Entities;
using Data.Abstract;
using Data.Context;
using Data.Repository;
using Microsoft.Extensions.Logging;

namespace Data.EntityFramework
{
    public class EFCartItemDal : GenericRepository<CartItem>, ICartItemDal
    {
        public EFCartItemDal(DatabaseContext context, IQueryContext queryContext, ILogger<GenericRepository<CartItem>> logger) : base(context, queryContext, logger)
        {
        }
    }
}
