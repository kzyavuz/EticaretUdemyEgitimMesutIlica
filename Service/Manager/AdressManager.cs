using Core.Entities;
using Data.Abstract;
using Service.Service;

namespace Service.Manager
{
    public class AdressManager : GenericManager<Adress>, IAdressService
    {
        public AdressManager(IUnitOfWorkDal unitOfWork) : base(unitOfWork)
        {
        }
    }
}
