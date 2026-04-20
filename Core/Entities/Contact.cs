using Core.Abstract;
using System.ComponentModel;

namespace Core.Entities
{
    public class Contact: IBaseEntity
    {
        [DisplayName("İletişim Başlığı")]
        public string Title { get; set; } = string.Empty!;

        [DisplayName("Adres")]
        public string? Adress { get; set; }

        [DisplayName("Telefon")]
        public string? Phone { get; set; }

        [DisplayName("E-posta")]
        public string? Email { get; set; }

        [DisplayName("Çalışma Saatleri")]
        public string? WorkingHours { get; set; }

        [DisplayName("Instagram")]
        public string? Instagram { get; set; }

        [DisplayName("WhatsApp")]
        public string? WhatsApp { get; set; }

        [DisplayName("YouTube")]
        public string? YouTube { get; set; }

        [DisplayName("LinkedIn")]
        public string? LinkedIn { get; set; }
    }
}
