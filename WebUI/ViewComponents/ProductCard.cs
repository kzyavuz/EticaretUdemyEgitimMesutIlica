using Core.Entities;
using Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUI.Models;
using Microsoft.AspNetCore.Identity;
using Service.Extensions;
using Service.Service; // ICartService için
using Core.Dto; // Cart ve CartLine için

namespace WebUI.ViewComponents
{
    public class ProductCard : ViewComponent
    {
        private readonly DatabaseContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ProductCard(DatabaseContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(Product product)
        {
            bool isFavorite = false;
            bool isInCart = false;

            // 1. FAVORİ KONTROLÜ
            if (FunctionHelper.IsLoggedIn())
            {
                var user = await _userManager.GetUserAsync(User as System.Security.Claims.ClaimsPrincipal);
                if (user != null)
                {
                    isFavorite = await _context.Favories
                        .AnyAsync(f => f.AppUserId == user.Id && f.ProductId == product.Id);
                }
            }
            else
            {
                var sessionFavorites = HttpContext.Session.GetJson<List<Product>>("GetFavorites");
                isFavorite = sessionFavorites?.Any(f => f.Id == product.Id) ?? false;
            }

            // 2. SEPET KONTROLÜ (IsInCart)
            if (FunctionHelper.IsLoggedIn())
            {
                var user = await _userManager.GetUserAsync(User as System.Security.Claims.ClaimsPrincipal);
                if (user != null)
                {
                    // DB'deki sepetinde bu ürün var mı?
                    isInCart = await _context.CartItems // Tablo isminiz CartItems olduğunu varsayıyorum
                        .AnyAsync(c => c.AppUserId == user.Id && c.ProductId == product.Id);
                }
            }
            else
            {
                // Session'daki sepetinde bu ürün var mı?
                var sessionCart = HttpContext.Session.GetJson<Cart>("Cart");
                isInCart = sessionCart?.CardLines?.Any(l => l.Product.Id == product.Id) ?? false;
            }

            return View(new ProductCardViewModel
            {
                Product = product,
                IsFavorite = isFavorite,
                IsInCart = isInCart // Yeni eklediğimiz özellik
            });
        }
    }
}