using Core.Entities;
using Data.Abstract;
using Service.Service;

namespace Service.Manager
{
    public class ContactManager : GenericManager<Contact>, IContactService
    {
        public ContactManager(IUnitOfWorkDal unitOfWork) : base(unitOfWork)
        {
        }
    }
}
