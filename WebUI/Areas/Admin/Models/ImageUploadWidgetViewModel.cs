namespace WebUI.Areas.Admin.Models
{
    public class ImageUploadWidgetViewModel
    {
        public string Label { get; set; } = "Görsel";
        public string FieldName { get; set; } = "Image";
        public string? CurrentValue { get; set; }
        public bool IsEdit { get; set; }
    }
}
