using Core.Entities;
using Data.Abstract;
using Service.Service;

namespace Service.Manager
{
    public class BrandManager : GenericManager<Brand>, IBrandService
    {
        public BrandManager(IUnitOfWorkDal unitOfWork) : base(unitOfWork)
        {
        }
    }
}
