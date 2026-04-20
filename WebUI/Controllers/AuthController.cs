using Core.Dto;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Service.Extensions.Abstract;
using Service.Service;
using System.Text;
using WebUI.Models.Login;

namespace WebUI.Controllers
{
    [AllowAnonymous]
    public class AuthController : BaseController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IMailService _mailService;
        private readonly IMemoryCache _memoryCache;
        private readonly IAuthSecurityService _authSecurity;
        private readonly ICartService _cartService;

        // Gizli admin URL slug
        private const string AdminSlug = "admin-giris";

        public AuthController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IConfiguration configuration,
            IMemoryCache memoryCache,
            IMailService mailService,
            IAuthSecurityService authSecurity,
            ICartService cartService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _mailService = mailService;
            _cartService = cartService;
            _memoryCache = memoryCache;
            _authSecurity = authSecurity;
        }

        // ═══════════════════════════════════════════════
        //   GİRİŞ İŞLEMLERİ (USER & ADMIN)
        // ═══════════════════════════════════════════════

        [HttpGet("giris")]
        public IActionResult SignIn(string? returnUrl = null) => View(new SignInViewModel { IsAdmin = false, ReturnUrl = returnUrl });

        [EnableRateLimiting("LoginPolicy")]
        [HttpPost("giris")]
        public async Task<IActionResult> SignIn(SignInViewModel model, string? returnUrl = null)
        {
            model.IsAdmin = false;
            model.ReturnUrl = returnUrl;
            return await HandleSignInAsync(model, isAdmin: false);
        }

        [HttpGet(AdminSlug)]
        public IActionResult AdminSignIn() => View(new SignInViewModel { IsAdmin = true });

        [EnableRateLimiting("LoginPolicy")]
        [HttpPost(AdminSlug)]
        public async Task<IActionResult> AdminSignIn(SignInViewModel model)
        {
            model.IsAdmin = true;
            return await HandleSignInAsync(model, isAdmin: true);
        }

        // ═══════════════════════════════════════════════
        //   KAYIT (Sadece Standart Kullanıcı İçin)
        // ═══════════════════════════════════════════════

        [HttpGet("kayit")]
        public IActionResult SignUp() => View();

        [HttpPost("kayit")]
        public async Task<IActionResult> SignUp(SignUpViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                SetSweetAlertMessage("Hata!", "Bu e-posta adresi zaten kayıtlı.", "warning");
                return View(model);
            }

            var user = new AppUser
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                UserName = model.UserName,
                PhoneNumber = model.Phone,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                var roleResult = await _userManager.AddToRoleAsync(user, "User");
                if (roleResult.Succeeded)
                {
                    SetSweetAlertMessage("Başarılı!", "Kayıt işlemi başarılı.", "success");
                    return RedirectToAction(nameof(SignIn));
                }

                foreach (var error in roleResult.Errors)
                    ModelState.AddModelError("", $"Rol atama hatası: {error.Description}");

                await _userManager.DeleteAsync(user);
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // ═══════════════════════════════════════════════
        //   ÇIKIŞ
        // ═══════════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignOut()
        {
            await _signInManager.SignOutAsync();
            SetSweetAlertMessage("İşlem Başarılı", "Çıkış işlemi başarılı bir şekilde gerçekleşti.", "info");
            return RedirectToAction("Index", "Home");
        }

        // ═══════════════════════════════════════════════
        //   2FA DOĞRULAMA (ORTAK)
        // ═══════════════════════════════════════════════

        [HttpGet("dogrulama-kodu")]
        public IActionResult TwoFactor(string? userId, bool isAdmin = false)
        {
            if (string.IsNullOrEmpty(userId))
            {
                SetSweetAlertMessage("Hata!", "Giriş için doğrulama bilgisi bulunamadı.", "error");
                return RedirectToLogin(isAdmin);
            }

            if (!_memoryCache.TryGetValue(_authSecurity.GetTwoFactorCacheKey(userId), out _))
            {
                SetSweetAlertMessage("Hata!", "Aktif doğrulama kodu bulunamadı.", "warning");
                return RedirectToLogin(isAdmin);
            }

            return View(new TwoFactorViewModel { UserId = userId, IsAdmin = isAdmin });
        }


        [HttpPost("dogrulama-kodu")]
        public async Task<IActionResult> TwoFactor(TwoFactorViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);


                var cacheKey = _authSecurity.GetTwoFactorCacheKey(model.UserId);
                var attemptKey = GetTwoFactorAttemptKey(model.UserId);
                const int MaxAttempts = 5;

                // 1. Cache/Süre Kontrolü
                if (!_memoryCache.TryGetValue(cacheKey, out TwoFactorCacheDto? cacheEntry))
                {
                    SetSweetAlertMessage("Süre Doldu",
                        "Güvenlik nedeniyle doğrulama kodunun süresi dolmuştur (genellikle 2-5 dk). Lütfen tekrar giriş yaparak yeni bir kod isteyin.",
                        "warning");
                    return RedirectToLogin(model.IsAdmin);
                }

                // 2. Hatalı Deneme Sayısı Kontrolü
                int attempts = _memoryCache.TryGetValue(attemptKey, out int currentAttempts) ? currentAttempts : 0;

                if (attempts >= MaxAttempts)
                {
                    ClearTwoFactorCache(cacheKey, attemptKey);
                    SetSweetAlertMessage("Çok Fazla Deneme",
                        "Çok fazla hatalı kod girişi yaptınız. Güvenliğiniz için oturumunuz sonlandırıldı. Lütfen baştan başlayın.",
                        "error");
                    return RedirectToLogin(model.IsAdmin);
                }

                // 3. Kod Doğrulama
                if (!string.Equals(cacheEntry!.Code, model.Code?.Trim(), StringComparison.Ordinal))
                {
                    attempts++;
                    int remainingAttempts = MaxAttempts - attempts;
                    _memoryCache.Set(attemptKey, attempts, TimeSpan.FromMinutes(10));
                    if (remainingAttempts > 0)
                    {
                        SetSweetAlertMessage("Hatalı Kod",
                            $"Girdiğiniz doğrulama kodu yanlıştır. Lütfen kontrol edip tekrar deneyiniz. Kalan deneme hakkınız: {remainingAttempts}",
                            "error");
                        return View(model);
                    }
                    else
                    {
                        ClearTwoFactorCache(cacheKey, attemptKey);
                        SetSweetAlertMessage("Hesap Kilitlendi",
                            "Hatalı deneme sınırına ulaştınız. Lütfen giriş işlemini baştan başlatın.",
                            "error");
                        return RedirectToLogin(model.IsAdmin);
                    }
                }

                // 4. Başarılı Giriş
                ClearTwoFactorCache(cacheKey, attemptKey);
                var user = await _userManager.FindByIdAsync(model.UserId);

                if (user == null)
                {
                    SetSweetAlertMessage("Sistem Hatası", "Kullanıcı kaydı bulunamadı. Lütfen destek ekibiyle iletişime geçin.", "error");
                    return RedirectToLogin(model.IsAdmin);
                }

                await _signInManager.SignInAsync(user, cacheEntry.RememberMe);
                await _authSecurity.ResetUserFailuresAsync(user);

                var area = GetAndClearTwoFactorArea(model.UserId);

                // Başarı mesajı (Opsiyonel)
                SetSweetAlertMessage("Hoş Geldiniz", "Kimlik doğrulama başarılı.", "success");

                return RedirectToDashboard(area);
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("2fa-debug.log", $"[2FA-DEBUG] EXCEPTION: {ex.Message} - {ex.StackTrace}\n");
                throw;
            }
        }

        // ═══════════════════════════════════════════════
        //   ŞİFRE İŞLEMLERİ (ORTAK)
        // ═══════════════════════════════════════════════

        [HttpGet("sifremi-unuttum")]
        public IActionResult ForgotPassword(bool isAdmin = false)
            => View(new ForgotPasswordViewModel { IsAdmin = isAdmin });

        [EnableRateLimiting("LoginPolicy")]
        [HttpPost("sifremi-unuttum")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (!await ValidateCaptchaAsync())
            {
                SetSweetAlertMessage("Hata!", "Robot doğrulaması başarısız.", "error");
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                var resetLink = Url.Action(nameof(AResetPassword), "Auth",
                    new { token = encodedToken, email = user.Email, isAdmin = model.IsAdmin },
                    Request.Scheme);

                await _mailService.SendPasswordResetLinkAsync(user.Email!, resetLink!);
            }

            SetSweetAlertMessage("Başarılı!", "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi.", "success");
            return RedirectToLogin(model.IsAdmin);
        }

        [HttpGet("sifre-sifirla")]
        public IActionResult AResetPassword(string? token, string? email, bool isAdmin = false)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                SetSweetAlertMessage("Hata!", "Bağlantı geçersiz.", "error");
                return RedirectToLogin(isAdmin);
            }

            return View("ResetPassword", new ResetPasswordViewModel
            {
                Token = token,
                Email = email,
                IsAdmin = isAdmin
            });
        }

        [HttpPost("sifre-sifirla")]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                SetSweetAlertMessage("Başarılı!", "Şifreniz güncellendi.", "success");
                return RedirectToLogin(model.IsAdmin);
            }

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);

            if (result.Succeeded)
            {
                await _authSecurity.ResetUserFailuresAsync(user);
                SetSweetAlertMessage("Başarılı!", "Şifreniz güncellendi.", "success");
                return RedirectToLogin(model.IsAdmin);
            }

            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);

            return View(model);
        }

        // ███████████████████████████████████████████████
        //   PRIVATE YARDIMCI METODLAR
        // ███████████████████████████████████████████████

        private async Task<IActionResult> HandleSignInAsync(SignInViewModel model, bool isAdmin)
        {
            var viewName = isAdmin ? nameof(AdminSignIn) : nameof(SignIn);

            if (!ModelState.IsValid) return View(viewName, model);

            //if (!await ValidateCaptchaAsync())
            //{
            //    SetSweetAlertMessage("Hata!", "Robot doğrulaması başarısız.", "error");
            //    return View(viewName, model);
            //}

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null || !user.IsActive)
            {
                await IncrementIpFailCountAsync();
                SetSweetAlertMessage("Hata!", "Geçersiz e-posta veya şifre.", "error");
                return View(viewName, model);
            }

            var roles = await _userManager.GetRolesAsync(user);
            bool hasRequiredRole = isAdmin
                ? (roles.Contains("Admin") || roles.Contains("SuperAdmin"))
                : roles.Contains("User");

            if (!hasRequiredRole)
            {
                await _authSecurity.HandleFailedAttemptAsync(user);
                SetRemainingSecondsIfLocked(user);
                SetSweetAlertMessage("Hata!", "Geçersiz e-posta veya şifre.", "error");
                return View(viewName, model);
            }

            var lockRemaining = _authSecurity.GetLockRemainingSeconds(user, DateTime.UtcNow);
            if (lockRemaining.HasValue)
            {
                TempData["RemainingSeconds"] = lockRemaining.Value;
                return View(viewName, model);
            }

            var passwordResult = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);

            if (!passwordResult.Succeeded)
            {
                await _authSecurity.HandleFailedAttemptAsync(user);
                SetRemainingSecondsIfLocked(user);
                SetSweetAlertMessage("Hata!", "Geçersiz e-posta veya şifre.", "error");
                return View(viewName, model);
            }

            var area = isAdmin ? "Admin" : "Customer";

            if (user.TwoFactorEnabled)
            {
                await _authSecurity.StartTwoFactorAsync(user, model.RememberMe);
                StoreTwoFactorArea(user.Id.ToString(), area);
                SetSweetAlertMessage("Başarılı!", "Doğrulama kodu gönderildi.", "success");
                return RedirectToAction(nameof(TwoFactor), new { userId = user.Id, isAdmin = isAdmin });
            }

            await _signInManager.SignInAsync(user, model.RememberMe);
            await _authSecurity.ResetUserFailuresAsync(user);
            await _cartService.MigrateSessionCartToDb();

            if (!isAdmin && !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);
            return RedirectToDashboard(area);
        }

        private IActionResult RedirectToLogin(bool isAdmin)
        {
            return isAdmin
                ? RedirectToAction(nameof(AdminSignIn))
                : RedirectToAction(nameof(SignIn));
        }

        private async Task<bool> ValidateCaptchaAsync()
        {
            var captchaResponse = Request.Form["g-recaptcha-response"];
            if (string.IsNullOrEmpty(captchaResponse)) return false;

            var secret = _configuration["GoogleReCaptcha:SecretKey"];
            using var client = new HttpClient();
            var result = await client.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secret}&response={captchaResponse}",
                null);

            var json = await result.Content.ReadAsStringAsync();
            return json.Contains("\"success\": true");
        }

        private async Task IncrementIpFailCountAsync()
        {
            var ip = GetClientIp();
            var ipKey = $"login_fail_ip:{ip}";
            int fails = _memoryCache.TryGetValue(ipKey, out int tmp) ? tmp : 0;
            fails++;
            _memoryCache.Set(ipKey, fails, TimeSpan.FromMinutes(15));
            await Task.Delay(fails > 20 ? 1000 : 500);
        }

        private void SetRemainingSecondsIfLocked(AppUser user)
        {
            var remaining = _authSecurity.GetLockRemainingSeconds(user, DateTime.UtcNow);
            if (remaining.HasValue)
                TempData["RemainingSeconds"] = remaining.Value;
        }

        private void StoreTwoFactorArea(string userId, string area)
            => _memoryCache.Set($"2fa_area:{userId}", area, TimeSpan.FromMinutes(10));

        private string GetAndClearTwoFactorArea(string userId)
        {
            var key = $"2fa_area:{userId}";
            var area = _memoryCache.TryGetValue(key, out string? val) ? val ?? "Customer" : "Customer";
            _memoryCache.Remove(key);
            return area;
        }

        private void ClearTwoFactorCache(string cacheKey, string attemptKey)
        {
            _memoryCache.Remove(cacheKey);
            _memoryCache.Remove(attemptKey);
        }

        private IActionResult RedirectToDashboard(string area)
            => RedirectToAction("Index", "Dashboard", new { area });

        private string GetTwoFactorAttemptKey(string userId) => $"2fa_attempts:{userId}";

        private string GetClientIp()
            => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}