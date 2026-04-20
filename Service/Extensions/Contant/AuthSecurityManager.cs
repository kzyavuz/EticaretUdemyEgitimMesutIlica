using System.Security.Cryptography;
using Core.Dto;
using Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Service.Extensions.Abstract;

namespace Service.Extensions.Contant
{
    public class AuthSecurityManager : IAuthSecurityService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IMemoryCache _memoryCache;
        private readonly IMailService _mailService;

        private const string TwoFactorCacheKeyPrefix = "2fa:";

        public AuthSecurityManager(
            UserManager<AppUser> userManager,
            IMemoryCache memoryCache,
            IMailService mailService)
        {
            _userManager = userManager;
            _memoryCache = memoryCache;
            _mailService = mailService;
        }

        public int? GetLockRemainingSeconds(AppUser user, DateTime now)
        {
            if (user.FailedLoginCount <= 0 || !user.LastFailedLogin.HasValue)
                return null;

            var delay = GetDelayForFailedCount(user.FailedLoginCount);
            var nextTry = user.LastFailedLogin.Value.Add(delay);

            if (now >= nextTry)
                return null;

            return (int)(nextTry - now).TotalSeconds;
        }

        public async Task HandleFailedAttemptAsync(AppUser user)
        {
            user.FailedLoginCount++;
            user.LastFailedLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }

        public async Task ResetUserFailuresAsync(AppUser user)
        {
            user.FailedLoginCount = 0;
            user.LastFailedLogin = null;
            await _userManager.UpdateAsync(user);
        }

        public async Task StartTwoFactorAsync(AppUser user, bool rememberMe)
        {
            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var expires = DateTime.UtcNow.AddMinutes(5);

            var entry = new TwoFactorCacheDto
            {
                Code = code,
                ExpiresUtc = expires,
                RememberMe = rememberMe
            };

            var cacheKey = GetTwoFactorCacheKey(user.Id.ToString());
            var cacheDuration = expires - DateTime.UtcNow;

            _memoryCache.Set(cacheKey, entry, cacheDuration);

            // Mail gönder
            await _mailService.SendTwoFactorCodeAsync(user.Email!, code);
        }

        public string GetTwoFactorCacheKey(string userId)
            => $"{TwoFactorCacheKeyPrefix}{userId}";

        public TimeSpan GetDelayForFailedCount(int failedCount) =>
            failedCount switch
            {
                1 => TimeSpan.FromSeconds(10),
                2 => TimeSpan.FromSeconds(15),
                3 => TimeSpan.FromSeconds(20),
                4 => TimeSpan.FromSeconds(30),
                5 => TimeSpan.FromSeconds(40),
                _ => TimeSpan.FromMinutes(1)
            };
    }
}
