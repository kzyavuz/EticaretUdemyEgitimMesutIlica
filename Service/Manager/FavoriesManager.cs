using Core.Entities;
using Data.Abstract;
using Service.Service;

namespace Service.Manager
{
    public class FavoriesManager : GenericManager<Favories>, IFavoriesService
    {
        public FavoriesManager(IUnitOfWorkDal unitOfWork) : base(unitOfWork)
        {
        }
    }
}
