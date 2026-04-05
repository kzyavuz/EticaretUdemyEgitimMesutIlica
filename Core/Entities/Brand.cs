using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Entities
{
    public class Brand : IBaseEntity
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Logo { get; set; }

        public ICollection<Product>? Products { get; set; }
    }
}
