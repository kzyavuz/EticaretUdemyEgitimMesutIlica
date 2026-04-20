using System.ComponentModel.DataAnnotations;

namespace WebUI.Areas.Customer.Models
{
    public class EditProfileViewModel
    {
        [Display(Name = "Ad Soyad")]
        public string? FullName => $"{FirstName} {LastName}";

        [Display(Name = "Kullanıcı Adı")]
        public string? UserName { get; set; }

        [Display(Name = "Ad")]
        [Required(ErrorMessage = "{0} boş olamaz")]
        public string FirstName { get; set; }

        [Display(Name = "Soyad")]
        [Required(ErrorMessage = "{0} boş olamaz")]
        public string LastName { get; set; }

        [Display(Name = "E-posta Adresi")]
        [Required(ErrorMessage = "{0} boş olamaz")]
        public string Email { get; set; }

        [Display(Name = "Telefon Numarası")]
        [Required(ErrorMessage = "{0} boş olamaz")]
        public string Phone { get; set; }

        [Display(Name = "İki Faktörlü Doğrulama")]
        public bool IsTwoFactor {get; set; }
        public string? ProfilImage { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
