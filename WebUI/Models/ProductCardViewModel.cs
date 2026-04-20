using Core.Entities;

namespace WebUI.Models
{
    public class ProductCardViewModel
    {
        public Product Product { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsInCart { get; set; }
    }
}