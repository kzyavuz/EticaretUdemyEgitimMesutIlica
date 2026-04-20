using System.ComponentModel.DataAnnotations;
namespace Core.Abstract
{
    public interface IFeatureable
    {
        [Display(Name = "Öne Çıkarma Durumu")]
        public bool IsFeatured { get; set; }
    }
}
