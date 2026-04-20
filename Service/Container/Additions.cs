using Core.Abstract;
using Data.Abstract;
using Data.EntityFramework;
using Data.Repository;
using Microsoft.Extensions.DependencyInjection;
using Service.Extensions.Abstract;
using Service.Extensions.Contant;
using Service.Manager;
using Service.Service;

namespace Service.Container
{
    public static class Additions
    {
        public static void ContainerDependencies(this IServiceCollection services)
        {
            // UnitOfWork ve Generic Repo
            services.AddScoped<IQueryContext, QueryContext>();
            services.AddScoped<IUnitOfWorkDal, UnitOfWork>();
            services.AddScoped(typeof(IGenericDal<>), typeof(GenericRepository<>));
            services.AddScoped(typeof(IGenericService<>), typeof(GenericManager<>));

            // Helper Tanımları
            services.AddScoped<IMailService, MailManager>();
            services.AddScoped<IAuthSecurityService, AuthSecurityManager>();

            // entityler için servis tanımları

            services.AddScoped<IBrandService, BrandManager>();
            services.AddScoped<IBrandDal, EFBrandDal>();

            services.AddScoped<ICategoryService, CategoryManager>();
            services.AddScoped<ICategoryDal, EFCategoryDal>();

            services.AddScoped<IContactService, ContactManager>();
            services.AddScoped<IContactDal, EFContactDal>();

            services.AddScoped<IFavoriesService, FavoriesManager>();
            services.AddScoped<IFavoriesDal, EFFavoriesDal>();

            services.AddScoped<IIncomingMessageService, IncomingMessageManager>();
            services.AddScoped<IIncomingMessageDal, EFIncomingMessageDal>();

            services.AddScoped<INewsService, NewsManager>();
            services.AddScoped<INewsDal, EFNewsDal>();

            services.AddScoped<IProductService, ProductManager>();
            services.AddScoped<IProductDal, EFProductDal>();

            services.AddScoped<ISliderService, SliderManager>();
            services.AddScoped<ISliderDal, EFSliderDal>();

            services.AddScoped<ICartService, CartManager>();
            services.AddScoped<ICartItemDal, EFCartItemDal>();

            services.AddScoped<IAdressService, AdressManager>();
            services.AddScoped<IAdressDal, EFAdressDal>();
        }
    }
}
