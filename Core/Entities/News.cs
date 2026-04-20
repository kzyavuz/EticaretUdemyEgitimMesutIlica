using Core.Abstract;
using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class News : IBaseEntity
    {
        [Display(Name = "Kampanya Başlığı")]
        [Required(ErrorMessage = "Kampanya başlığı zorunludur.")]
        [StringLength(200, ErrorMessage = "Kampanya başlığı en fazla 200 karakter olabilir.")]
        public string Title { get; set; }

        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Display(Name = "Kampanya Görseli")]
        public string? Image { get; set; }
    }
}
