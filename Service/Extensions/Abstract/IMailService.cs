using Core.Entities;

namespace Service.Extensions.Abstract
{
    public interface IMailService
    {
        Task SendContactMailAsync(IncomingMessage model);
        Task SendTwoFactorCodeAsync(string email, string code);
        Task SendPasswordResetLinkAsync(string email, string resetLink);
    }
}
