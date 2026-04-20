using Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Service.Service;
using System.Diagnostics;
using System.Linq.Expressions;
using WebUI.Models;

namespace WebUI.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ISliderService _sliderService;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IBrandService _brandService;

        public HomeController(ISliderService sliderService, IProductService productService, ICategoryService categoryService, IBrandService brandService)
        {
            _sliderService = sliderService;
            _productService = productService;
            _categoryService = categoryService;
            _brandService = brandService;
        }

        public async Task<IActionResult> Index()
        {
            HomeViewModel? model = new()
            {
                Sliders = await _sliderService.GetListAsync(),

                Products = await _productService.GetListAsync(
                    filter: p => p.IsHome,
                    orderBy: p => p.OrderNumber ?? 0,
                    descending: true,
                    take: 60,
                    includes: new Expression<Func<Product, object>>[] {
                        p => p.Category,
                        p => p.Brand
                    }
                ),

                Categories = await _categoryService.GetListAsync(
                    orderBy: c => c.OrderNumber ?? 0,
                    descending: true
                ),

                Brands = await _brandService.GetListAsync(
                    orderBy: b => b.OrderNumber ?? 0,
                    descending: true
                )
            };

            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
