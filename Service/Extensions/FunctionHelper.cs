
using Core.Dto;
using Core.Enum;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Service.Service;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Service.Extensions
{
    public static class FunctionHelper
    {
        private static IHttpContextAccessor? _httpContextAccessor;
        private static ICartService? _cartService;

        // Program.cs'de bir kez çağır
        public static void Configure(IHttpContextAccessor httpContextAccessor, ICartService cartService)
        {
            _httpContextAccessor = httpContextAccessor;
            _cartService = cartService;
        }

        public static bool IsActive(DataStatus status)
        {
            if (status == DataStatus.Active)
            {
                return true;
            }

            return false;
        }

        public static bool IsPublic(DataStatus status)
        {
            return status == DataStatus.Active;
        }

        public static bool IsDraft(DataStatus status)
        {
            return status == DataStatus.Draft;
        }

        public static string GetDisplayName(this System.Enum enumValue)
        {
            return enumValue.GetType()
                .GetMember(enumValue.ToString())
                .FirstOrDefault()
                ?.GetCustomAttribute<DisplayAttribute>()
                ?.Name ?? enumValue.ToString();
        }

        public static string StatusBadge(DataStatus status)
        {
            var displayName = status.GetType()
                .GetField(status.ToString())
                ?.GetCustomAttribute<DisplayAttribute>()
                ?.Name ?? status.ToString();

            var colorClass = status switch
            {
                DataStatus.Active => "bg-primary",
                DataStatus.Draft => "bg-secondary",
                _ => "bg-secondary"
            };

            return $"<span class='badge rounded-pill {colorClass}'>{displayName}</span>";
        }

        public static string GenerateSlug(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var turkishChars = new Dictionary<char, string>
            {
                { 'ç', "c" }, { 'Ç', "c" },
                { 'ğ', "g" }, { 'Ğ', "g" },
                { 'ı', "i" }, { 'İ', "i" },
                { 'ö', "o" }, { 'Ö', "o" },
                { 'ş', "s" }, { 'Ş', "s" },
                { 'ü', "u" }, { 'Ü', "u" }
            };

            var sb = new System.Text.StringBuilder();
            foreach (char c in text)
            {
                sb.Append(turkishChars.TryGetValue(c, out string replacement) ? replacement : c.ToString());
            }
            text = sb.ToString();
            text = text.ToLowerInvariant();
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", "-");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"[^a-z0-9\-]", string.Empty);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"-{2,}", "-");
            text = text.Trim('-');
            return text;
        }

        public static string? ResolveImageSrc(string? value)
        {

            if (string.IsNullOrWhiteSpace(value))
                return "/img/default-image.webp";

            if (value.StartsWith("http://") || value.StartsWith("https://"))
                return value;

            if (value.StartsWith("uploads/"))
                return "/" + value;

            return "/img/no-image.jpg";
        }

        public static bool IsLoggedIn(params string[] roles)
        {
            var user = _httpContextAccessor?.HttpContext?.User;

            // Kullanıcı giriş yapmamış
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            // Rol parametresi gelmemiş, sadece giriş kontrolü yeterli
            if (roles.Length == 0)
                return true;

            // Giriş yapılmış, şimdi rol kontrolü
            return roles.Any(role => user.IsInRole(role));
        }

        public static void SetJson(this ISession session, string key, object value)
        {
            var jsonString = JsonConvert.SerializeObject(value);
            session.SetString(key, jsonString); // ← artık bulunur
        }

        public static T? GetJson<T>(this ISession session, string key) where T : class
        {
            var data = session.GetString(key); // ← artık bulunur
            return data == null ? default(T) : JsonConvert.DeserializeObject<T>(data);
        }

        public static async Task<bool> HasCartItemsAsync()
        {
            var httpContext = _httpContextAccessor?.HttpContext;

            if (httpContext == null) return false;

            if (IsLoggedIn())
            {
                if (_cartService == null) return false;

                var cart = await _cartService.GetCartLines();
                return cart?.CardLines?.Any() == true;
            }
            
            var sessionCart = httpContext.Session.GetJson<Cart>("Cart");
            return sessionCart?.CardLines?.Any() == true;
        }

    }
}
