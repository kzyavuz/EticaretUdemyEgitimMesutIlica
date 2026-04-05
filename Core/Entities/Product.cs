using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Entities
{
    public class Product: IBaseEntity
    {
        public string Title { get; set; }

        public string? Description { get; set; }

        public string? Image { get; set; }

        public string? ProductCode { get; set; }

        public decimal Price { get; set; }

        public int Stock { get; set; }

        public bool IsHome { get; set; }

        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public int? BrandId { get; set; }
        public Brand? Brand { get; set; }
    }
}
