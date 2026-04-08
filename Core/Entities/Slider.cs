using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class Slider : IBaseEntity
    {
        [Display(Name = "Başlık")]
        public string? Title { get; set; }

        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Display(Name = "Slider Görseli")]
        public string? Image { get; set; }

        [Display(Name = "Link")]
        public string? Link { get; set; }
    }
}
