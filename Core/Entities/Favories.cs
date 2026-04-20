using Core.Abstract;
using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class Favories : IBaseEntity
    {
        [Display(Name = "Kullanıcı")]
        public int AppUserId { get; set; }
        public AppUser? AppUser { get; set; }

        [Display(Name = "Ürün")]
        public int ProductId { get; set; }
        public Product? Product { get; set; }
    }
}
