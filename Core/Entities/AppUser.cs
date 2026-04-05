using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class AppUser: IdentityUser<int>
    {
        [Display(Name = "Ad")]
        public string? FirstName { get; set; }

        [Display(Name = "Soyad")]
        public string? LastName { get; set; }

        [Display(Name = "Profil Fotoğrafı")]
        public string? Image { get; set; }

        [Display(Name = "Aktiflik Durumu")]
        public bool IsActive { get; set; } = true;

        public int FailedLoginCount { get; set; }

        public DateTime? LastFailedLogin { get; set; }
    }
}
    