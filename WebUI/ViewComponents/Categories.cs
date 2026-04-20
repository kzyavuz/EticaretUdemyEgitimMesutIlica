using Core.Entities;
using Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUI.Models;

namespace WebUI.ViewComponents
{
    public class Categories : ViewComponent
    {
        private readonly DatabaseContext _context;

        public Categories(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var allCategories = await _context.Categories
                .Where(c => c.ISTopMenu)
                .ToListAsync();

            var directCounts = await _context.Products
                .Where(p => p.CategoryId.HasValue)
                .GroupBy(p => p.CategoryId!.Value)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.CategoryId, g => g.Count);

            // Katman ayrımı
            var l1 = allCategories.Where(c => c.ParentId == 0).ToList();
            var l1Ids = l1.Select(c => c.Id).ToHashSet();
            var l2 = allCategories.Where(c => c.ParentId != 0 && l1Ids.Contains(c.ParentId)).ToList();
            var l2Ids = l2.Select(c => c.Id).ToHashSet();
            var l3 = allCategories.Where(c => c.ParentId != 0 && l2Ids.Contains(c.ParentId)).ToList();

            // Ürün sayıları: L3 doğrudan, L2 = doğrudan + L3 toplamı, L1 = doğrudan + L2 toplamı
            var productCounts = new Dictionary<int, int>(directCounts);

            foreach (var l2cat in l2)
            {
                int l3Total = l3.Where(c => c.ParentId == l2cat.Id)
                                .Sum(c => directCounts.GetValueOrDefault(c.Id, 0));
                productCounts[l2cat.Id] = directCounts.GetValueOrDefault(l2cat.Id, 0) + l3Total;
            }

            foreach (var l1cat in l1)
            {
                int l2Total = l2.Where(c => c.ParentId == l1cat.Id)
                                .Sum(c => productCounts.GetValueOrDefault(c.Id, 0));
                productCounts[l1cat.Id] = directCounts.GetValueOrDefault(l1cat.Id, 0) + l2Total;
            }

            var model = new CategoryMenuViewModel
            {
                MainCategories = l1,
                SubCategories = l2,
                SubSubCategories = l3,
                ProductCounts = productCounts
            };

            return View(model);
        }
    }
}
