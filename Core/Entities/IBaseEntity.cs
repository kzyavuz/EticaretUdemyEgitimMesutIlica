using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public abstract class IBaseEntity
    {
        [Display(Name = "Id")]
        public int Id { get; set; }

        [Display(Name = "Sıra No")]
        public int? OrderNumber { get; set; }

        [Display(Name = "Aktif Mi?")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Güncellenme Tarihi")]
        public DateTime? UpdatedDate { get; set; }

        [Display(Name = "Silinme Tarihi")]
        public DateTime? DeletedDate { get; set; }
    }
}
