using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.Login
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Gecerli bir e-posta adresi girin.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yeni sifrenizi girin.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Sifre en az 6 karakter olmali.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Sifre")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Sifre (Tekrar)")]
        [Compare("Password", ErrorMessage = "Sifreler eslesmiyor.")]
        public string ConfirmPassword { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
    }
}
