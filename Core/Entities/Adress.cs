using Core.Abstract;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class Adress : IBaseEntity
    {
        [DisplayName("Adres Başlığı")]
        [Required(ErrorMessage = "{0} alanı boş geçilemez.")]
        public string Title { get; set; }

        [DisplayName("Alıcı Ad Soyad")]
        public string FullName => $"{RecipientName} {RecipientSurname}".Trim();

        [DisplayName("Alıcı Adı")]
        [Required(ErrorMessage = "{0} alanı boş geçilemez.")]
        public string RecipientName { get; set; }

        [DisplayName("Alıcı Soyadı")]
        [Required(ErrorMessage = "{0} alanı boş geçilemez.")]
        public string RecipientSurname { get; set; }

        [DisplayName("Telefon Numarası")]
        [Required(ErrorMessage = "{0} alanı boş geçilemez.")]
        public string PhoneNumber { get; set; }

        [DisplayName("Sehir")]
        [Required(ErrorMessage = "{0} alanı boş geçilemez.")]
        public string Province { get; set; }

        [DisplayName("İlçe")]
        [Required(ErrorMessage = "{0} alanı boş geçilemez.")]
        public string District { get; set; }

        [DisplayName("Mahalle")]
        [Required(ErrorMessage = "{0} alanı boş geçilemez.")]
        public string Neighbourhood { get; set; }

        [DisplayName("Adres Tarifi")]
        public string Description { get; set; }

        [DisplayName("Fatura Adresi")]
        public bool IsBillingAdress { get; set; } = false;

        [DisplayName("Teslimat Adresi")]
        public bool IsDeliveryAdress { get; set; } = false;

        public Guid AddressGuid { get; set; }

        public int AppuserId { get; set; }
        public AppUser AppUser { get; set; }
    }
}
