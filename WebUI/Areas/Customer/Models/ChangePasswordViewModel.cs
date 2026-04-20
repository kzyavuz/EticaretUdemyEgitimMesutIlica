using System.ComponentModel.DataAnnotations;

namespace WebUI.Areas.Customer.Models
{
    public class ChangePasswordViewModel
    {
        [Display(Name = "Geçerli Sifre")]
        [Required(ErrorMessage = "Geçerli Sifreyi giriniz.")]
        public string CurrentPassword { get; set; }

        [Display(Name = "Yeni Sifre")]
        [Required(ErrorMessage = "Yeni Sifreyi giriniz.")]
        public string NewPassword { get; set; }

        [Display(Name = "Yeni Sifre Tekrarı")]
        [Required(ErrorMessage = "Yeni Sifreyi tekrar giriniz."), Compare("NewPassword")]
        public string ConfirmPassword { get; set; }
    }
}
