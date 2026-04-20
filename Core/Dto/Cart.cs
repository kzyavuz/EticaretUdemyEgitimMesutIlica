using Core.Entities;

namespace Core.Dto
{
    // Core/Models/Cart.cs (Session için basit bir nesne)
    public class Cart
    {
        public List<CartLine> CardLines { get; set; } = new();
        public decimal TotalPrice => CardLines.Sum(x => x.TotalPrice);
        public decimal TaxTotalPrice => TotalPrice * 0.20m;
        public decimal SubTotalPrice => TotalPrice - TaxTotalPrice;
    }

    public class CartLine
    {
        public Product Product { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => Product.Price * Quantity;
    }
}
