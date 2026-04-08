using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using SImage = SixLabors.ImageSharp.Image;

namespace WebUI.Helper
{
    public class FileHelper
    {
        private readonly IWebHostEnvironment _env;
        private readonly string _wwwRootPath;

        // Image settings
        private const int JpegQuality = 80;
        private const int WebpQuality = 80;
        private const int MaxImageWidth = 2048;
        private const int MaxImageHeight = 2048;
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
        private const int CopyBufferSize = 81920; // 80 KB - optimal for file operations

        // MIME type validation
        private static readonly Dictionary<string, string> AllowedMimeTypes = new()
        {
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".webp", "image/webp" }
        };

        public FileHelper(IWebHostEnvironment env)
        {
            _env = env;
            _wwwRootPath = _env.WebRootPath;
        }

        public async Task<string?> UploadAsync(IFormFile file, string root)
        {
            if (!ValidateFile(file))
                return null;

            EnsureDirectoryExists(root);

            string originalExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string fullPath = Path.Combine(root, GenerateUniqueFileName(originalExtension));

            try
            {
                string savedFileName;
                
                if (IsImageFile(originalExtension))
                {
                    savedFileName = await ProcessAndSaveImageAsync(file, fullPath, originalExtension);
                }
                else
                {
                    await SaveFileAsync(file, fullPath);
                    savedFileName = Path.GetFileName(fullPath);
                }

                return GetRelativePath(root, savedFileName);
            }
            catch (Exception ex)
            {
                // Hata durumunda yüklenen dosyayı sil
                try
                {
                    if (File.Exists(fullPath))
                        File.Delete(fullPath);
                    // JPG olarak kaydedilmiş olabilir
                    string jpgPath = Path.ChangeExtension(fullPath, ".jpg");
                    if (File.Exists(jpgPath))
                        File.Delete(jpgPath);
                }
                catch { }
                
                // Hata mesajı debug amacıyla kalır, ama null döndür
                System.Diagnostics.Debug.WriteLine($"Dosya yükleme hatası: {ex.Message}");
                return null;
            }
        }

        public void Delete(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return;

            try
            {
                var fullPath = Path.Combine(_wwwRootPath, relativePath.TrimStart('/', '\\'));
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch
            {
                // Log the error if needed
            }
        }

        public async Task<string?> UpdateAsync(IFormFile newFile, string root, string? oldRelativePath)
        {
            var newPath = await UploadAsync(newFile, root);

            if (newPath != null && !string.IsNullOrEmpty(oldRelativePath))
            {
                Delete(oldRelativePath);
            }

            return newPath;
        }

        private async Task<string> ProcessAndSaveImageAsync(IFormFile file, string fullPath, string originalExtension)
        {
            using (var image = await SImage.LoadAsync(file.OpenReadStream()))
            {
                // Resim boyutunu optimize et
                if (image.Width > MaxImageWidth || image.Height > MaxImageHeight)
                {
                    var newHeight = (int)((double)image.Height / image.Width * MaxImageWidth);
                    image.Mutate(x => x.Resize(MaxImageWidth, newHeight, KnownResamplers.Lanczos3));
                }

                // Format'a göre kaydet
                if (originalExtension == ".png")
                {
                    await image.SaveAsPngAsync(fullPath, new PngEncoder { CompressionLevel = PngCompressionLevel.DefaultCompression });
                    return Path.GetFileName(fullPath);
                }
                else if (originalExtension == ".gif")
                {
                    await image.SaveAsGifAsync(fullPath);
                    return Path.GetFileName(fullPath);
                }
                else if (originalExtension == ".webp")
                {
                    await image.SaveAsWebpAsync(fullPath, new WebpEncoder { Quality = WebpQuality });
                    return Path.GetFileName(fullPath);
                }
                else
                {
                    // JPG olarak kaydet (daha küçük dosya boyutu)
                    string jpgPath = Path.ChangeExtension(fullPath, ".jpg");
                    await image.SaveAsJpegAsync(jpgPath, new JpegEncoder { Quality = JpegQuality });
                    return Path.GetFileName(jpgPath);
                }
            }
        }

        private async Task SaveFileAsync(IFormFile file, string fullPath)
        {
            using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, CopyBufferSize))
            {
                await file.CopyToAsync(stream);
            }
        }

        private bool ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            if (file.Length > MaxFileSizeBytes)
                return false;

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedMimeTypes.ContainsKey(extension))
                return false;

            // MIME type kontrolü - WhatsApp dosyaları için daha esnek kontrol
            // Bazı cihazlar yanlış MIME type gönderebilir, sadece 'image' içerdiğini kontrol et
            if (!string.IsNullOrEmpty(file.ContentType))
            {
                if (!file.ContentType.Contains("image", StringComparison.OrdinalIgnoreCase))
                    return false; // İmaj değilse reddet
            }

            return true;
        }

        private void EnsureDirectoryExists(string root)
        {
            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);
        }

        private string GenerateUniqueFileName(string originalExtension)
        {
            return $"{Guid.NewGuid():N}{originalExtension}";
        }

        private string GetRelativePath(string root, string fileName)
        {
            string relativeRoot = Path.GetRelativePath(_wwwRootPath, root);
            return Path.Combine(relativeRoot, fileName).Replace("\\", "/");
        }

        private bool IsImageFile(string extension)
        {
            return AllowedMimeTypes.ContainsKey(extension);
        }
    }
}