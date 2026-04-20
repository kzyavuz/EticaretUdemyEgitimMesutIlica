using Core.Dto;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Service;

namespace WebUI.Areas.Customer.Controllers
{
    [AllowAnonymous]
    public class CartController : CustomerBaseController
    {
        private readonly ICartService _cartService;
        private readonly IProductService _productService;

        public CartController(ICartService cartService, IProductService productService)
        {
            _cartService = cartService;
            _productService = productService;
        }

        [HttpGet("Sepetim")]
        public async Task<IActionResult> Index()
        {
            var lines = await _cartService.GetCartLines();
            return View(lines);
        }

        // Sepete ürün ekle
        [HttpPost("SepeteEkle")]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            Product product = await _productService.GetByIdAsync(productId);

            CartLine cartLine = new()
            {
                Quantity = quantity,
                Product = product
            };

            var result = await _cartService.AddToCart(cartLine);
            if (result)
            {
                return Ok(new { message = "Ürün başarıyla eklendi" });
            }
            return BadRequest(new { message = "Ürün eklenirken hata oluştu" });
        }

        // Sepetten ürün sil
        [HttpPost("SepettenSil")]
        public async Task<IActionResult> RemoveCart(int productId)
        {
            var result = await _cartService.RemoveFromCart(productId);
            if (result)
            {
                return Ok(new { message = "Ürün başarıyla silindi" });
            }
            return BadRequest(new { message = "Ürün silinirken hata oluştu" });
        }

        // Sepeti temizle
        [HttpPost("SepetiTemizle")]
        public async Task<IActionResult> Clear()
        {
            var result = await _cartService.Clear();
            if (result)
            {
                return Ok(new { message = "Sepet başarıyla temizlendi" });
            }
            return BadRequest(new { message = "Sepet temizlenirken hata oluştu" });
        }

        [HttpGet("SiparisOzet")]
        public async Task<IActionResult> GetCartSummary()
        {
            var cart = await _cartService.GetCartLines();

            return Json(new
            {
                subTotal = cart.SubTotalPrice.ToString("C2"),
                tax = cart.TaxTotalPrice.ToString("C2"),
                total = cart.TotalPrice.ToString("C2"),
                shipping = cart.TotalPrice > 1000 || cart.TotalPrice == 0
                   ? "Kargo Bedava"
                   : "Kargo Ücreti: " + (100m).ToString("C2")
            });
        }

        [HttpPost("MiktarGuncelle")]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            var result = await _cartService.UpdateQuantity(productId, quantity);
            if (result)
            {
                return Ok();
            }
            return BadRequest();
        }
    }
}