namespace WebUI.Areas.Admin.Models
{
    public class BreadcrumbItem
    {
        public string Title { get; set; }
        public string? Controller { get; set; }
        public string? Action { get; set; }
    }
}
