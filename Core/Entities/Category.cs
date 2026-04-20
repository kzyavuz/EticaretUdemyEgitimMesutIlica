using Core.Abstract;
using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class Category : IBaseEntity
    {
        [Display(Name = "Kategori Adı")]
        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        [StringLength(150, ErrorMessage = "Kategori adı en fazla 150 karakter olabilir.")]
        public string Title { get; set; }

        [Display(Name = "Üst Kategori")]
        public int ParentId { get; set; }

        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Display(Name = "Kategori Görseli")]
        public string? Image { get; set; }

        [Display(Name = "Üst Menüde Göster")]
        public bool ISTopMenu { get; set; }
    
        public ICollection<Product>? Products { get; set; }
    }
}
