using Core.Entities;
using Data.Abstract;
using Service.Service;

namespace Service.Manager
{
    public class ProductManager : GenericManager<Product>, IProductService
    {
        public ProductManager(IUnitOfWorkDal unitOfWork) : base(unitOfWork)
        {
        }
    }
}
