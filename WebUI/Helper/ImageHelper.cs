namespace WebUI.Helper
{
    public static class ImageHelper
    {
        /// <summary>
        /// Verilen değeri geçerli bir img src URL'ye dönüştürür.
        /// - http/https ile başlıyorsa doğrudan döner.
        /// - uploads/ ile başlıyorsa başına / ekler.
        /// - Diğer durumlarda null döner (geçersiz değer).
        /// </summary>
        public static string? ResolveImageSrc(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (value.StartsWith("http://") || value.StartsWith("https://"))
                return value;

            if (value.StartsWith("uploads/"))
                return "/" + value;

            return null;
        }
    }
}
