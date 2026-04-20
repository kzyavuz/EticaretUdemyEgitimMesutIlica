using Core.Entities;
using Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Service;
using System.Linq.Expressions;
using WebUI.Models;

namespace WebUI.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;


        public ProductsController(IProductService productService, ICategoryService categoryService)
        {
            _productService = productService;
            _categoryService = categoryService;
        }

        public async Task<IActionResult> Index(int? categoryId)
        {
            IQueryable<Product> query = _productService.Queryable();

            if (categoryId.HasValue)
            {
                var categoryIds = await _categoryService.Queryable()
                    .Where(c => c.Id == categoryId || c.ParentId == categoryId)
                    .Select(c => c.Id)
                    .ToListAsync();

                query = query.Where(x => x.CategoryId.HasValue && categoryIds.Contains(x.CategoryId.Value));
            }

            IEnumerable<Product> data = await query
                .Include(x => x.Category)
                .Include(x => x.Brand)
                .OrderByDescending(x => x.OrderNumber)
                .ToListAsync();

            ViewBag.Count = data.Count();
            return View(data);
        }

        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            Product? product = await _productService.GetFirstAsync(
                filter: x => x.Id == id,
                includes: new Expression<Func<Product, object>>[] {
                    p => p.Category,
                    p => p.Brand
                }
            );

            if (product == null)
            {
                return NotFound();
            }

            List<Product>? relatedProducts = await _productService.GetListAsync(
                filter: x => x.Id != id && x.CategoryId == product.CategoryId,
                take: 12,
                includes: new Expression<Func<Product, object>>[] {
                    p => p.Category,
                    p => p.Brand
                }
            );

            ProductDetailViewModel data = new()
            {
                Product = product,
                RelatedProducts = relatedProducts
            };

            return View(data);
        }
    }
}
