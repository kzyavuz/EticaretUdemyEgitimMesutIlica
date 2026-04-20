
using System.Security.Claims;
using Core.Entities;
using Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Service.Extensions;

namespace WebUI.Areas.Customer.Controllers
{
    [AllowAnonymous]
    public class FavoritesController : CustomerBaseController
    {
        private readonly DatabaseContext _context;

        public FavoritesController(DatabaseContext databaseContext)
        {
            _context = databaseContext;
        }

        [HttpGet("favorilerim")]
        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (FunctionHelper.IsLoggedIn() && int.TryParse(userIdClaim, out int userId))
            {
                var favorites = await _context.Favories
                    .Where(f => f.AppUserId == userId && f.Product != null)
                    .Select(f => f.Product!)
                    .ToListAsync();

                return View(favorites);
            }

            return View(GetFavorites());
        }

        [HttpPost("ekle")]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Add(int ProductId)
        {
            var favorites = GetFavorites();
            var product = _context.Products.Find(ProductId);

            if (product != null && !favorites.Any(f => f.Id == ProductId))
            {
                favorites.Add(product);
                HttpContext.Session.SetJson("GetFavorites", favorites);

                if (FunctionHelper.IsLoggedIn())
                {
                    var userIdStr = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdStr, out int userId))
                    {
                        bool exists = _context.Favories.Any(f => f.AppUserId == userId && f.ProductId == ProductId);
                        if (!exists)
                        {
                            var fav = new Favories
                            {
                                AppUserId = userId,
                                ProductId = ProductId
                            };
                            _context.Favories.Add(fav);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }

            return Ok();
        }

        [HttpPost("cikar")]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Remove(int ProductId)
        {
            var favorites = GetFavorites();
            var product = favorites.FirstOrDefault(f => f.Id == ProductId);

            if (product != null)
            {
                favorites.Remove(product);
                HttpContext.Session.SetJson("GetFavorites", favorites);
            }

            if (FunctionHelper.IsLoggedIn())
            {
                var userIdStr = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    var fav = await _context.Favories.FirstOrDefaultAsync(f => f.AppUserId == userId && f.ProductId == ProductId);
                    if (fav != null)
                    {
                        _context.Favories.Remove(fav);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            return Ok();
        }


        private List<Product> GetFavorites()
        {
            return HttpContext.Session.GetJson<List<Product>>("GetFavorites") ?? new List<Product>();
        }
    }
}
