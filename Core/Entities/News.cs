using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class News : IBaseEntity
    {
        [Display(Name = "Haber Başlığı")]
        [Required(ErrorMessage = "Haber başlığı zorunludur.")]
        [StringLength(200, ErrorMessage = "Haber başlığı en fazla 200 karakter olabilir.")]
        public string Title { get; set; }

        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Display(Name = "Haber Görseli")]
        public string? Image { get; set; }
    }
}
