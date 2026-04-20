using Core.Entities;
using Data.Abstract;
using Service.Service;

namespace Service.Manager
{
    public class IncomingMessageManager : GenericManager<IncomingMessage>, IIncomingMessageService
    {
        public IncomingMessageManager(IUnitOfWorkDal unitOfWork) : base(unitOfWork)
        {
        }
    }
}
