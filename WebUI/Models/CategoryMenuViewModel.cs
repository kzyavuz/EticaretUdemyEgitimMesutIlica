using Core.Entities;

namespace WebUI.Models
{
    public class CategoryMenuViewModel
    {
        public List<Category>? MainCategories { get; set; }    // L1: ParentId == 0
        public List<Category>? SubCategories { get; set; }    // L2: ParentId == L1.Id
        public List<Category>? SubSubCategories { get; set; } // L3: ParentId == L2.Id
        public Dictionary<int, int>? ProductCounts { get; set; }
    }
}
