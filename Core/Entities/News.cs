using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Entities
{
    public class News : IBaseEntity
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Image { get; set; }
    }
}
