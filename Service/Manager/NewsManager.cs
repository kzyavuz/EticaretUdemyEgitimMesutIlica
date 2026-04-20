using Core.Entities;
using Data.Abstract;
using Service.Service;

namespace Service.Manager
{
    public class NewsManager : GenericManager<News>, INewsService
    {
        public NewsManager(IUnitOfWorkDal unitOfWork) : base(unitOfWork)
        {
        }
    }
}
