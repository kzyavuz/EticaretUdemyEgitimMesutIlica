
using Core.Entities;

namespace Service.Extensions.Abstract
{
    public interface IAuthSecurityService
    {
        int? GetLockRemainingSeconds(AppUser user, DateTime now);
        Task HandleFailedAttemptAsync(AppUser user);
        Task ResetUserFailuresAsync(AppUser user);
        Task StartTwoFactorAsync(AppUser user, bool rememberMe);
        string GetTwoFactorCacheKey(string userId);
        TimeSpan GetDelayForFailedCount(int failedCount);
    }
}
