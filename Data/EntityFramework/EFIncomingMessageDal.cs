using Core.Abstract;
using Core.Entities;
using Data.Abstract;
using Data.Context;
using Data.Repository;
using Microsoft.Extensions.Logging;

namespace Data.EntityFramework
{
    public class EFIncomingMessageDal : GenericRepository<IncomingMessage>, IIncomingMessageDal
    {
        public EFIncomingMessageDal(DatabaseContext context, IQueryContext queryContext, ILogger<GenericRepository<IncomingMessage>> logger) : base(context, queryContext, logger)
        {
        }
    }
}
