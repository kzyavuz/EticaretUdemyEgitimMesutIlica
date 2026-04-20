using Core.Abstract;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Entities
{
    public class CartItem : IBaseEntity
    {
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public int Quantity { get; set; } = 0;
    }
}
