using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class Brand : IBaseEntity
    {
        [Display(Name = "Marka Adı")]
        [Required(ErrorMessage = "Marka adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Marka adı en fazla 100 karakter olabilir.")]
        public string Name { get; set; }

        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Display(Name = "Logo")]
        public string? Logo { get; set; }

        public ICollection<Product>? Products { get; set; }
    }
}
