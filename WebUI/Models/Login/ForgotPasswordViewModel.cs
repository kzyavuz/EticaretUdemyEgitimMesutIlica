using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.Login
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Lutfen e-posta adresinizi girin.")]
        [EmailAddress(ErrorMessage = "Gecerli bir e-posta adresi girin.")]
        [Display(Name = "E-posta")]
        public string? Email { get; set; }
        public bool IsAdmin { get; set; }
    }
}
