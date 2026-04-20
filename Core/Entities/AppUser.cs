using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities
{
    public class AppUser : IdentityUser<int>
    {

        [Display(Name = "Ad Soyad")]
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        [Display(Name = "Ad")]
        [Required(ErrorMessage = "Ad zorunludur.")]
        [StringLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir.")]
        public string? FirstName { get; set; }

        [Display(Name = "Soyad")]
        [Required(ErrorMessage = "Soyad zorunludur.")]
        [StringLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir.")]
        public string? LastName { get; set; }

        [Display(Name = "Profil Fotoğrafı")]
        public string? Image { get; set; }

        [Display(Name = "Kullanıcı Adı")]
        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [StringLength(50, ErrorMessage = "Kullanıcı adı en fazla 50 karakter olabilir.")]
        public override string? UserName { get; set; }

        [Display(Name = "E-posta")]
        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        public override string? Email { get; set; }

        [Display(Name = "Telefon Numarası")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası girin.")]
        public override string? PhoneNumber { get; set; }

        [Display(Name = "Kullanıcı Kimliği")]
        public Guid? UserGuid { get; set; } = Guid.NewGuid();

        [Display(Name = "Aktiflik Durumu")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "basarısız giris sayısı"), ScaffoldColumn(false)]
        public int FailedLoginCount { get; set; }

        [Display(Name = "En son basarısız giris tarihi"), ScaffoldColumn(false)]
        public DateTime? LastFailedLogin { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Güncellenme Tarihi")]
        public DateTime? UpdatedDate { get; set; }

        [Display(Name = "Silinme Tarihi")]
        public DateTime? DeletedDate { get; set; }

        public ICollection<Favories>? Favories { get; set; }
        public ICollection<Adress>? Adresses { get; set; }

    }
}
