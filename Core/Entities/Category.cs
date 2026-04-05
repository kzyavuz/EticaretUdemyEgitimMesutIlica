using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Entities
{
    public class Category: IBaseEntity
    {
        public int ParentId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Image { get; set; }
        public bool ISTopMenu { get; set; }

        public ICollection<Product>? Products { get; set; }
    }
}
