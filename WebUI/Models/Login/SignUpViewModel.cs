using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.Login
{
    public class SignUpViewModel
    {
        [Display(Name = "Ad")]
        [Required(ErrorMessage = "Lütfen adını yazınız")]
        public string? FirstName { get; set; }

        [Display(Name = "Soyad")]
        [Required(ErrorMessage = "Lütfen Soyadınızı yazınız")]
        public string? LastName { get; set; }

        [Display(Name = "Kullanıcı Adı")]
        public string? UserName { get; set; }

        [Required(ErrorMessage = "Lütfen E Postasınızı yazınız")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-mail adresi giriniz.")]
        [Display(Name = "E Posta")]
        public string? Email { get; set; }

        [Display(Name = "Telefon")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Şifrenizi giriniz.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        [Display(Name = "Sifreniz")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Şifre tekrarını giriniz.")]
        [Compare("Password", ErrorMessage = "Şifreler aynı değil.")]
        [Display(Name = "Sifre Tekrarı")]
        public string? ConfirmPassword { get; set; }
        public bool IsAdmin { get; set; }
    }
}
