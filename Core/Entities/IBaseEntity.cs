using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Entities
{
    public abstract class IBaseEntity
    {
        public int Id { get; set; }

        public int? OrderNo { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime UpdatedDate { get; set; }

        public DateTime DeletedDate { get; set; }
    }
}
