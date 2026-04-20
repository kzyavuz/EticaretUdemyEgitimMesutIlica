using Core.Dto;

namespace Service.Service
{
    public interface ICartService
    {
        Task<bool> AddToCart(CartLine cartLine);
        Task<bool> UpdateQuantity(int productId, int quantity);
        Task<bool> RemoveFromCart(int productId);
        Task<Cart> GetCartLines();
        Task<bool> Clear();
        Task MigrateSessionCartToDb();
    }
}
