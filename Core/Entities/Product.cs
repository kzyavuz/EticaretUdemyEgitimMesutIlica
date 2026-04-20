using Core.Abstract;
using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class Product : IBaseEntity
    {
        [Display(Name = "Ürün Adı")]
        [Required(ErrorMessage = "Ürün adı zorunludur.")]
        [StringLength(200, ErrorMessage = "Ürün adı en fazla 200 karakter olabilir.")]
        public string Title { get; set; }

        [Display(Name = "Açıklama")]
        [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
        public string? Description { get; set; }

        [Display(Name = "Ürün Görseli")]
        public string? Image { get; set; }

        [Display(Name = "Ürün Kodu")]
        [StringLength(50, ErrorMessage = "Ürün kodu en fazla 50 karakter olabilir.")]
        public string? ProductCode { get; set; }

        [Display(Name = "Fiyat")]
        [Range(0, double.MaxValue, ErrorMessage = "Fiyat sıfır veya daha büyük olmalıdır.")]
        public decimal Price { get; set; }

        [Display(Name = "Stok Sayısı")]
        [Range(0, int.MaxValue, ErrorMessage = "Stok sayısı sıfır veya daha büyük olmalıdır.")]
        public int StockCount { get; set; }

        [Display(Name = "Anasayfada Gösterilsin Mi?")]
        public bool IsHome { get; set; }

        [Display(Name = "Kategori")]
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        [Display(Name = "Marka")]
        public int? BrandId { get; set; }
        public Brand? Brand { get; set; }

        public ICollection<Favories>? Favories { get; set; }
    }
}
