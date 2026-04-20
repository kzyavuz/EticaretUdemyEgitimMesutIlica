using Core.Abstract;
using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class IncomingMessage : IBaseEntity
    {
        [Display(Name = "Ad Soyad"), Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
        public string FullName { get; set; }

        [Display(Name = "Email"), Required(ErrorMessage = "Email alanı zorunludur."), EmailAddress(ErrorMessage = "Geçersiz email formatı.")]
        public string Email { get; set; }

        [Display(Name = "Telefon")]
        public string? Phone { get; set; }

        [Display(Name = "Konu"), Required(ErrorMessage = "Konu alanı zorunludur.")]
        public string Subject { get; set; }

        [Display(Name = "Mesaj"), Required(ErrorMessage = "Mesaj alanı zorunludur.")]
        public string Message { get; set; }

        [Display(Name = "Okundu")]
        public bool IsRead { get; set; } = false;

        [Display(Name = "Okunma Tarihi")]
        public DateTime? ReadDate { get; set; }
    }
}
