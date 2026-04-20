using Core.Abstract;
using Core.Entities;
using Data.Abstract;
using Data.Context;
using Data.Repository;
using Microsoft.Extensions.Logging;

namespace Data.EntityFramework
{
    public class EFSliderDal : GenericRepository<Slider>, ISliderDal
    {
        public EFSliderDal(DatabaseContext context, IQueryContext queryContext, ILogger<GenericRepository<Slider>> logger) : base(context, queryContext, logger)
        {
        }
    }
}
