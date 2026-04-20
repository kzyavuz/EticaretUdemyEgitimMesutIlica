using System.ComponentModel.DataAnnotations;

namespace WebUI.Areas.Customer.Models
{
    public class ProfileViewModel
    {
        public EditProfileViewModel EditProfileViewModel { get; set; }
        public ChangePasswordViewModel ChangePasswordViewModel { get; set; }
    }
}
