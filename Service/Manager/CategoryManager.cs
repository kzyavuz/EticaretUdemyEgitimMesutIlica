using Core.Entities;
using Data.Abstract;
using Service.Service;

namespace Service.Manager
{
    public class CategoryManager : GenericManager<Category>, ICategoryService
    {
        public CategoryManager(IUnitOfWorkDal unitOfWork) : base(unitOfWork)
        {
        }
    }
}
