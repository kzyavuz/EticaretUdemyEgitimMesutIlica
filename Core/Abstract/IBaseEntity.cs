using Core.Enum;
using System.ComponentModel.DataAnnotations;

namespace Core.Abstract
{
    public abstract class IBaseEntity
    {
        [Display(Name = "Id")]
        public int Id { get; set; }

        [Display(Name = "Slug")]
        public string? Slug { get; set; }

        [Display(Name = "Sıra No")]
        public int? OrderNumber { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Güncellenme Tarihi")]
        public DateTime? UpdatedDate { get; set; }

        [Display(Name = "Silinme Tarihi")]
        public DateTime? DeletedDate { get; set; }

        [Display(Name = "Durum(1: Aktif, 2: Taslak)")]
        public DataStatus Status { get; set; } = DataStatus.Active;

    }
}