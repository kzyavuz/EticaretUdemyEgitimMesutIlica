namespace WebUI.Models
{
    public class ImageUploadWidgetViewModel
    {
        /// <summary>Kartın başlığı, ör: "Logo", "Görsel", "Resim"</summary>
        public string Label { get; set; } = "Görsel";

        /// <summary>
        /// Form field adı. File input name, URL hidden name prefix olarak kullanılır.
        /// Ör: "Logo" → file name="Logo", hidden name="LogoUrl", delete flag name="DeleteLogo"
        /// </summary>
        public string FieldName { get; set; } = "Image";

        /// <summary>Mevcut kayıtlı değer (URL veya sunucu yolu)</summary>
        public string? CurrentValue { get; set; }

        /// <summary>Düzenleme modunda mı?</summary>
        public bool IsEdit { get; set; }
    }
}
