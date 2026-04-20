using Core.Dto;
using Core.Entities;
using Data.Abstract;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Service.Extensions;
using Service.Service;
using System.Security.Claims;

namespace Service.Manager;

public class CartManager : ICartService
{
    private readonly IUnitOfWorkDal _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CartManager> _logger;

    public CartManager(IUnitOfWorkDal unitOfWork, IHttpContextAccessor httpContextAccessor, ILogger<CartManager> logger)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    // Yardımcı Özellikler
    private ISession Session => _httpContextAccessor.HttpContext?.Session;
    private int UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out int id) ? id : 0;
        }
    }

    public async Task<bool> AddToCart(CartLine cartLine)
    {
        try
        {
            if (FunctionHelper.IsLoggedIn())
            {
                var repo = _unitOfWork.Repository<CartItem>();
                var existing = await repo.TGetFirstAsync(x => x.AppUserId == UserId && x.ProductId == cartLine.Product.Id);

                if (existing != null)
                {
                    existing.Quantity += cartLine.Quantity;
                    await repo.TUpdateAsync(existing);
                }
                else
                {
                    await repo.TInsertAsync(new CartItem
                    {
                        AppUserId = UserId,
                        ProductId = cartLine.Product.Id,
                        Quantity = cartLine.Quantity
                    });
                }
                return await _unitOfWork.SaveChangesAsync() > 0;
            }
            else
            {
                var cart = Session.GetJson<Cart>("Cart") ?? new Cart();
                var line = cart.CardLines.FirstOrDefault(x => x.Product.Id == cartLine.Product.Id);

                if (line != null) line.Quantity += cartLine.Quantity;
                else cart.CardLines.Add(new CartLine { Product = cartLine.Product, Quantity = cartLine.Quantity });

                Session.SetJson("Cart", cart);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sepete ekleme işlemi sırasında hata oluştu. ProductId: {ProductId}", cartLine.Product.Id);
            return false;
        }
    }


    public async Task<bool> UpdateQuantity(int productId, int quantity)
    {
        try
        {
            // Eğer miktar 0 veya daha az ise ürünü sepetten sil
            if (quantity <= 0)
            {
                return await RemoveFromCart(productId);
            }

            if (FunctionHelper.IsLoggedIn())
            {
                var repo = _unitOfWork.Repository<CartItem>();
                var item = await repo.TGetFirstAsync(x => x.AppUserId == UserId && x.ProductId == productId);
                if (item != null)
                {
                    item.Quantity = quantity; // Miktarı direkt set et
                    await repo.TUpdateAsync(item);
                    return await _unitOfWork.SaveChangesAsync() > 0;
                }
                return false;
            }
            else
            {
                var cart = Session.GetJson<Cart>("Cart") ?? new Cart();
                var line = cart.CardLines.FirstOrDefault(x => x.Product.Id == productId);
                if (line != null)
                {
                    line.Quantity = quantity; // Session'daki miktarı güncelle
                    Session.SetJson("Cart", cart);
                    return true;
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Miktar güncellenirken hata: ProductId {Id}", productId);
            return false;
        }
    }

    public async Task<Cart> GetCartLines()
    {
        try
        {
            if (FunctionHelper.IsLoggedIn())
            {
                var cartItems = await _unitOfWork.Repository<CartItem>().TGetListAsync(
                    filter: x => x.AppUserId == UserId,
                    includes: x => x.Product);

                return new Cart
                {
                    CardLines = cartItems
                        .Select(x => new CartLine { Product = x.Product, Quantity = x.Quantity })
                        .ToList()
                };
            }

            // Giriş yapılmamışsa session'dan getir
            var cart = Session.GetJson<Cart>("Cart") ?? new Cart();
            return cart;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sepet listesi alınırken hata oluştu.");
            return new Cart();
        }
    }

    public async Task<bool> RemoveFromCart(int productId)
    {
        try
        {
            if (FunctionHelper.IsLoggedIn())
            {
                var repo = _unitOfWork.Repository<CartItem>();
                var item = await repo.TGetFirstAsync(x => x.AppUserId == UserId && x.ProductId == productId);

                if (item != null)
                {
                    await repo.TRemoveAsync(item);
                    return await _unitOfWork.SaveChangesAsync() > 0;
                }
                return false;
            }
            else
            {
                var cart = Session.GetJson<Cart>("Cart") ?? new Cart();
                cart.CardLines.RemoveAll(x => x.Product.Id == productId);
                Session.SetJson("Cart", cart);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sepetten ürün silinirken hata oluştu. ProductId: {ProductId}", productId);
            return false;
        }
    }

    public async Task<bool> Clear()
    {
        try
        {
            if (FunctionHelper.IsLoggedIn())
            {
                var repo = _unitOfWork.Repository<CartItem>();
                var items = await repo.TGetListAsync(x => x.AppUserId == UserId);

                foreach (var item in items)
                    await repo.TRemoveAsync(item);

                return await _unitOfWork.SaveChangesAsync() > 0;
            }
            else
            {
                Session.Remove("Cart");
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sepet temizlenirken hata oluştu. UserId: {UserId}", UserId);
            return false;
        }
    }

    public async Task MigrateSessionCartToDb()
    {
        // 1. Session'daki sepeti al
        var sessionCart = Session.GetJson<Cart>("Cart");

        // Eğer session'da ürün yoksa veya kullanıcı login değilse işlem yapma
        if (sessionCart == null || !sessionCart.CardLines.Any() || !FunctionHelper.IsLoggedIn())
            return;

        try
        {
            var repo = _unitOfWork.Repository<CartItem>();

            foreach (var line in sessionCart.CardLines)
            {
                // Veritabanında bu kullanıcının sepetinde aynı ürün var mı?
                var existingItem = await repo.TGetFirstAsync(x => x.AppUserId == UserId && x.ProductId == line.Product.Id);

                if (existingItem != null)
                {
                    // Varsa miktarını artır
                    existingItem.Quantity += line.Quantity;
                    await repo.TUpdateAsync(existingItem);
                }
                else
                {
                    // Yoksa yeni ürün olarak ekle
                    await repo.TInsertAsync(new CartItem
                    {
                        AppUserId = UserId,
                        ProductId = line.Product.Id,
                        Quantity = line.Quantity
                    });
                }
            }

            // Veritabanına kaydet
            await _unitOfWork.SaveChangesAsync();

            // 2. İşlem başarılı olduktan sonra Session'daki sepeti temizle
            Session.Remove("Cart");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sepet transferi sırasında hata oluştu. UserId: {UserId}", UserId);
        }
    }
}