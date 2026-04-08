using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class IncomingMessage : IBaseEntity
    {
        [Display(Name = "Ad")]
        public string Name { get; set; }

        [Display(Name = "Soyad")]
        public string Surname { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Telefon")]
        public string? Phone { get; set; }

        [Display(Name = "Konu")]
        public string Subject { get; set; }

        [Display(Name = "Mesaj")]
        public string Message { get; set; }

        [Display(Name = "Okundu")]
        public bool IsRead { get; set; } = false;

        [Display(Name = "Okunma Tarihi")]
        public DateTime? ReadDate { get; set; }
    }
}
