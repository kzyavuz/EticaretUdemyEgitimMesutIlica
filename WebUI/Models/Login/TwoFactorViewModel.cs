
namespace WebUI.Models.Login
{
    public class TwoFactorViewModel
    {
        public string UserId { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
    }
}
