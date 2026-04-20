using System.ComponentModel.DataAnnotations;

namespace Core.Enum
{
    public enum DataStatus
    {
        [Display(Name = "Aktif")]
        Active = 1,      // Taslak (henüz yayında değil)

        [Display(Name = "Taslak")]
        Draft = 2,     // Aktif / Yayında

    }
}
