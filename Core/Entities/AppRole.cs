using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class AppRole : IdentityRole<int>
    {
        [Display(Name = "Rol Adı")]
        [Required(ErrorMessage = "Rol adı zorunludur.")]
        [StringLength(50, ErrorMessage = "Rol adı en fazla 50 karakter olabilir.")]
        public override string? Name { get; set; }
    }
}