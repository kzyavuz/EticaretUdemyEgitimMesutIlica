
using Core.Entities;

namespace WebUI.Models
{
    public class HomeViewModel
    {
        public List<Slider>? Sliders { get; set; }
        public List<Product>? Products { get; set; }
        public List<Category>? Categories { get; set; }
        public List<Brand>? Brands { get; set; }
    }
}