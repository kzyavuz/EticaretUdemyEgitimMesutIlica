using Core.Entities;
using Data.Abstract;
using Service.Service;

namespace Service.Manager
{
    public class SliderManager : GenericManager<Slider>, ISliderService
    {
        public SliderManager(IUnitOfWorkDal unitOfWork) : base(unitOfWork)
        {
        }
    }
}
