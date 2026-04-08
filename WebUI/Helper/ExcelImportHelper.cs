using OfficeOpenXml;
using Core.Entities;
using Data.Context;
using Microsoft.AspNetCore.SignalR;
using WebUI.Hubs;

namespace WebUI.Helper
{
    public class ExcelImportHelper
    {
        private readonly DatabaseContext _context;
        private readonly IHubContext<ImportProgressHub> _hubContext;

        public ExcelImportHelper(DatabaseContext context, IHubContext<ImportProgressHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Excel dosyasından Brands verilerini içeri aktar
        /// Beklenen sütunlar: Name, Description, Logo, IsActive
        /// </summary>
        public async Task<(int successCount, int updateCount, List<string> errors)> ImportBrandsAsync(IFormFile file)
        {
            var errors = new List<string>();
            int successCount = 0;
            int updateCount = 0;
            var brandsToAdd = new List<Brand>();
            var brandsToUpdate = new List<Brand>();

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            errors.Add("Excel dosyasında çalışma sayfası bulunamadı.");
                            return (0, 0, errors);
                        }

                        int rowCount = worksheet.Dimension?.Rows ?? 0;
                        if (rowCount < 2)
                        {
                            errors.Add("Excel dosyasında veri bulunamadı. Başlığından sonra en az 1 satır verisi olmalı.");
                            return (0, 0, errors);
                        }

                        // Başlık satırını kontrol et (Row 1)
                        var headers = new Dictionary<string, int>();
                        for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                        {
                            var headerValue = worksheet.Cells[1, col].Value?.ToString()?.ToLower().Trim();
                            if (!string.IsNullOrEmpty(headerValue))
                            {
                                headers[headerValue] = col;
                            }
                        }

                        // Gerekli sütunları kontrol et
                        var nameColumn = headers.Keys.FirstOrDefault(k =>k == "marka_adi");

                        if (nameColumn == null)
                        {
                            errors.Add("Excel dosyasında marka adı sütunu bulunamadı. Beklenen: 'marka_adi', 'Name', 'Adı'");
                            return (0, 0, errors);
                        }

                        var processedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        var descCol = headers.Keys.FirstOrDefault(k => k == "aciklama");

                        var logoCol = headers.Keys.FirstOrDefault(k => k == "logo");

                        var activeCol = headers.Keys.FirstOrDefault(k => k == "durum");

                        var orderNumberCol = headers.Keys.FirstOrDefault(k => k == "sira_numarasi");

                        // Verileri oku (Row 2 başlayarak)
                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                var name = worksheet.Cells[row, headers[nameColumn]].Value?.ToString()?.Trim();

                                if (string.IsNullOrEmpty(name))
                                {
                                    errors.Add($"Satır {row}: Marka adı boş olamaz.");
                                    continue;
                                }

                                // Aynı dosyada tekrarlayan satır kontrolü
                                if (!processedNames.Add(name))
                                {
                                    errors.Add($"Satır {row}: '{name}' bu dosyada birden fazla kez tekrarlıyor, ilk satır işlendi.");
                                    continue;
                                }

                                int? orderNumber = null;
                                if (orderNumberCol != null)
                                {
                                    var orderCell = worksheet.Cells[row, headers[orderNumberCol]].Value;
                                    if (orderCell != null)
                                    {
                                        try { orderNumber = Convert.ToInt32(orderCell); }
                                        catch
                                        {
                                            if (int.TryParse(orderCell.ToString()?.Trim(), out int parsed))
                                                orderNumber = parsed;
                                        }
                                    }
                                }

                                var description = descCol != null
                                    ? worksheet.Cells[row, headers[descCol]].Value?.ToString()?.Trim()
                                    : null;

                                var logo = logoCol != null
                                    ? worksheet.Cells[row, headers[logoCol]].Value?.ToString()?.Trim()
                                    : null;

                                var isActive = activeCol != null
                                    ? ParseBool(worksheet.Cells[row, headers[activeCol]].Value?.ToString() ?? "true")
                                    : true;

                                // Mevcut markayı bul → varsa güncelle, yoksa ekle
                                var existingBrand = _context.Brands.FirstOrDefault(b => b.Name == name);
                                if (existingBrand != null)
                                {
                                    existingBrand.Description = description;
                                    if (!string.IsNullOrEmpty(logo))
                                        existingBrand.Logo = logo;
                                    existingBrand.IsActive = isActive;
                                    if (orderNumber.HasValue)
                                        existingBrand.OrderNumber = orderNumber;
                                    existingBrand.UpdatedDate = DateTime.Now;
                                    brandsToUpdate.Add(existingBrand);
                                    updateCount++;
                                }
                                else
                                {
                                    brandsToAdd.Add(new Brand
                                    {
                                        Name = name,
                                        Description = description,
                                        Logo = logo,
                                        IsActive = isActive,
                                        OrderNumber = orderNumber
                                    });
                                    successCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Satır {row}: {ex.Message}");
                                continue;
                            }
                        }

                        int totalToProcess = brandsToAdd.Count + brandsToUpdate.Count;
                        int processed = 0;
                        int batchSize = 100;

                        // Yeni kayıtları batch halinde ekle
                        for (int i = 0; i < brandsToAdd.Count; i += batchSize)
                        {
                            var batch = brandsToAdd.Skip(i).Take(batchSize).ToList();
                            _context.Brands.AddRange(batch);
                            await _context.SaveChangesAsync();

                            processed += batch.Count;
                            var percentage = totalToProcess > 0 ? (int)((processed * 100) / totalToProcess) : 100;
                            await _hubContext.Clients.All.SendAsync("ReceiveProgress",
                                new { Processed = processed, Total = totalToProcess, Percentage = percentage, Message = $"{processed} / {totalToProcess} marka işlendi" });
                        }

                        // Mevcut kayıtları batch halinde güncelle
                        for (int i = 0; i < brandsToUpdate.Count; i += batchSize)
                        {
                            var batch = brandsToUpdate.Skip(i).Take(batchSize).ToList();
                            _context.Brands.UpdateRange(batch);
                            await _context.SaveChangesAsync();

                            processed += batch.Count;
                            var percentage = totalToProcess > 0 ? (int)((processed * 100) / totalToProcess) : 100;
                            await _hubContext.Clients.All.SendAsync("ReceiveProgress",
                                new { Processed = processed, Total = totalToProcess, Percentage = percentage, Message = $"{processed} / {totalToProcess} marka işlendi" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Insert(0, $"Genel hata: {ex.Message}");
                await _hubContext.Clients.All.SendAsync("ReceiveError", ex.Message);
                return (0, 0, errors);
            }

            await _hubContext.Clients.All.SendAsync("ReceiveComplete",
                new { SuccessCount = successCount, UpdateCount = updateCount, Errors = errors, TotalErrors = errors.Count });

            return (successCount, updateCount, errors);
        }

        /// <summary>
        /// Excel dosyasından Categories verilerini içeri aktar
        /// Beklenen sütunlar: Title, Description, Image, ISTopMenu, IsActive
        /// </summary>
        public async Task<(int successCount, int updateCount, List<string> errors)> ImportCategoriesAsync(IFormFile file)
        {
            var errors = new List<string>();
            int successCount = 0;
            int updateCount = 0;
            var categoriesToAdd = new List<Category>();
            var categoriesToUpdate = new List<Category>();

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            errors.Add("Excel dosyasında çalışma sayfası bulunamadı.");
                            return (0, 0, errors);
                        }

                        int rowCount = worksheet.Dimension?.Rows ?? 0;
                        if (rowCount < 2)
                        {
                            errors.Add("Excel dosyasında veri bulunamadı. Başlığından sonra en az 1 satır verisi olmalı.");
                            return (0, 0, errors);
                        }

                        // Başlık satırını kontrol et (Row 1)
                        var headers = new Dictionary<string, int>();
                        for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                        {
                            var headerValue = worksheet.Cells[1, col].Value?.ToString()?.ToLower().Trim();
                            if (!string.IsNullOrEmpty(headerValue))
                            {
                                headers[headerValue] = col;
                            }
                        }

                        // Gerekli sütunları kontrol et
                        if (!headers.ContainsKey("title"))
                        {
                            errors.Add("Excel dosyasında 'Title' sütunu bulunamadı.");
                            return (0, 0, errors);
                        }

                        // Verileri oku (Row 2 başlayarak)
                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                var title = worksheet.Cells[row, headers["title"]].Value?.ToString()?.Trim();

                                if (string.IsNullOrEmpty(title))
                                {
                                    errors.Add($"Satır {row}: Kategori başlığı boş olamaz.");
                                    continue;
                                }

                                var description = headers.ContainsKey("description")
                                    ? worksheet.Cells[row, headers["description"]].Value?.ToString()?.Trim()
                                    : null;
                                var image = headers.ContainsKey("image")
                                    ? worksheet.Cells[row, headers["image"]].Value?.ToString()?.Trim()
                                    : null;
                                var isTopMenu = headers.ContainsKey("istop menu")
                                    ? ParseBool(worksheet.Cells[row, headers["istop menu"]].Value?.ToString() ?? "false")
                                    : false;
                                var isActive = headers.ContainsKey("isactive")
                                    ? ParseBool(worksheet.Cells[row, headers["isactive"]].Value?.ToString() ?? "true")
                                    : true;

                                int? orderNumber = null;
                                if (headers.ContainsKey("sira_numarasi"))
                                {
                                    var orderCell = worksheet.Cells[row, headers["sira_numarasi"]].Value;
                                    if (orderCell != null)
                                    {
                                        try { orderNumber = Convert.ToInt32(orderCell); }
                                        catch
                                        {
                                            if (int.TryParse(orderCell.ToString()?.Trim(), out int parsed))
                                                orderNumber = parsed;
                                        }
                                    }
                                }

                                // Mevcut kaydı bul → varsa güncelle, yoksa ekle
                                var existing = categoriesToAdd.FirstOrDefault(c => c.Title == title)
                                               ?? _context.Categories.FirstOrDefault(c => c.Title == title);

                                if (existing != null && existing.Id > 0)
                                {
                                    existing.Description = description;
                                    if (!string.IsNullOrEmpty(image)) existing.Image = image;
                                    existing.ISTopMenu = isTopMenu;
                                    existing.IsActive = isActive;
                                    if (orderNumber.HasValue) existing.OrderNumber = orderNumber;
                                    existing.UpdatedDate = DateTime.Now;
                                    categoriesToUpdate.Add(existing);
                                    updateCount++;
                                }
                                else if (existing == null)
                                {
                                    categoriesToAdd.Add(new Category
                                    {
                                        Title = title,
                                        Description = description,
                                        Image = image,
                                        ISTopMenu = isTopMenu,
                                        IsActive = isActive,
                                        OrderNumber = orderNumber
                                    });
                                    successCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Satır {row}: {ex.Message}");
                                continue;
                            }
                        }

                        if (categoriesToAdd.Any())
                        {
                            _context.Categories.AddRange(categoriesToAdd);
                            await _context.SaveChangesAsync();
                        }
                        if (categoriesToUpdate.Any())
                        {
                            _context.Categories.UpdateRange(categoriesToUpdate);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Insert(0, $"Genel hata: {ex.Message}");
                return (0, 0, errors);
            }

            return (successCount, updateCount, errors);
        }

        /// <summary>
        /// Excel dosyasından News verilerini içeri aktar
        /// Beklenen sütunlar: Title, Description, Image, IsActive
        /// </summary>
        public async Task<(int successCount, int updateCount, List<string> errors)> ImportNewsAsync(IFormFile file)
        {
            var errors = new List<string>();
            int successCount = 0;
            int updateCount = 0;
            var newsToAdd = new List<News>();
            var newsToUpdate = new List<News>();

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            errors.Add("Excel dosyasında çalışma sayfası bulunamadı.");
                            return (0, 0, errors);
                        }

                        int rowCount = worksheet.Dimension?.Rows ?? 0;
                        if (rowCount < 2)
                        {
                            errors.Add("Excel dosyasında veri bulunamadı. Başlığından sonra en az 1 satır verisi olmalı.");
                            return (0, 0, errors);
                        }

                        // Başlık satırını kontrol et (Row 1)
                        var headers = new Dictionary<string, int>();
                        for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                        {
                            var headerValue = worksheet.Cells[1, col].Value?.ToString()?.ToLower().Trim();
                            if (!string.IsNullOrEmpty(headerValue))
                            {
                                headers[headerValue] = col;
                            }
                        }

                        // Gerekli sütunları kontrol et
                        if (!headers.ContainsKey("title"))
                        {
                            errors.Add("Excel dosyasında 'Title' sütunu bulunamadı.");
                            return (0, 0, errors);
                        }

                        // Verileri oku (Row 2 başlayarak)
                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                var title = worksheet.Cells[row, headers["title"]].Value?.ToString()?.Trim();

                                if (string.IsNullOrEmpty(title))
                                {
                                    errors.Add($"Satır {row}: Haber başlığı boş olamaz.");
                                    continue;
                                }

                                var description = headers.ContainsKey("description")
                                    ? worksheet.Cells[row, headers["description"]].Value?.ToString()?.Trim()
                                    : null;
                                var image = headers.ContainsKey("image")
                                    ? worksheet.Cells[row, headers["image"]].Value?.ToString()?.Trim()
                                    : null;
                                var isActive = headers.ContainsKey("isactive") || headers.ContainsKey("aktif")
                                    ? ParseBool(worksheet.Cells[row, headers.ContainsKey("isactive") ? headers["isactive"] : headers["aktif"]].Value?.ToString() ?? "true")
                                    : true;

                                int? orderNumber = null;
                                if (headers.ContainsKey("sira_numarasi"))
                                {
                                    var orderCell = worksheet.Cells[row, headers["sira_numarasi"]].Value;
                                    if (orderCell != null)
                                    {
                                        try { orderNumber = Convert.ToInt32(orderCell); }
                                        catch
                                        {
                                            if (int.TryParse(orderCell.ToString()?.Trim(), out int parsed))
                                                orderNumber = parsed;
                                        }
                                    }
                                }

                                // Mevcut kaydı bul → varsa güncelle, yoksa ekle
                                var existing = newsToAdd.FirstOrDefault(n => n.Title == title)
                                               ?? _context.News.FirstOrDefault(n => n.Title == title);

                                if (existing != null && existing.Id > 0)
                                {
                                    existing.Description = description;
                                    if (!string.IsNullOrEmpty(image)) existing.Image = image;
                                    existing.IsActive = isActive;
                                    if (orderNumber.HasValue) existing.OrderNumber = orderNumber;
                                    existing.UpdatedDate = DateTime.Now;
                                    newsToUpdate.Add(existing);
                                    updateCount++;
                                }
                                else if (existing == null)
                                {
                                    newsToAdd.Add(new News
                                    {
                                        Title = title,
                                        Description = description,
                                        Image = image,
                                        IsActive = isActive,
                                        OrderNumber = orderNumber
                                    });
                                    successCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Satır {row}: {ex.Message}");
                                continue;
                            }
                        }

                        if (newsToAdd.Any())
                        {
                            _context.News.AddRange(newsToAdd);
                            await _context.SaveChangesAsync();
                        }
                        if (newsToUpdate.Any())
                        {
                            _context.News.UpdateRange(newsToUpdate);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Insert(0, $"Genel hata: {ex.Message}");
                return (0, 0, errors);
            }

            return (successCount, updateCount, errors);
        }

        /// <summary>
        /// Excel dosyasından Sliders verilerini içeri aktar
        /// Beklenen sütunlar: Title, Description, Image, Link, IsActive
        /// </summary>
        public async Task<(int successCount, int updateCount, List<string> errors)> ImportSlidersAsync(IFormFile file)
        {
            var errors = new List<string>();
            int successCount = 0;
            int updateCount = 0;
            var slidersToAdd = new List<Slider>();
            var slidersToUpdate = new List<Slider>();

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            errors.Add("Excel dosyasında çalışma sayfası bulunamadı.");
                            return (0, 0, errors);
                        }

                        int rowCount = worksheet.Dimension?.Rows ?? 0;
                        if (rowCount < 2)
                        {
                            errors.Add("Excel dosyasında veri bulunamadı. Başlığından sonra en az 1 satır verisi olmalı.");
                            return (0, 0, errors);
                        }

                        // Başlık satırını kontrol et (Row 1)
                        var headers = new Dictionary<string, int>();
                        for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                        {
                            var headerValue = worksheet.Cells[1, col].Value?.ToString()?.ToLower().Trim();
                            if (!string.IsNullOrEmpty(headerValue))
                            {
                                headers[headerValue] = col;
                            }
                        }

                        // Gerekli sütunları kontrol et
                        if (!headers.ContainsKey("title"))
                        {
                            errors.Add("Excel dosyasında 'Title' sütunu bulunamadı.");
                            return (0, 0, errors);
                        }

                        // Verileri oku (Row 2 başlayarak)
                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                var title = worksheet.Cells[row, headers["title"]].Value?.ToString()?.Trim();

                                if (string.IsNullOrEmpty(title))
                                {
                                    errors.Add($"Satır {row}: Slider başlığı boş olamaz.");
                                    continue;
                                }

                                var description = headers.ContainsKey("description")
                                    ? worksheet.Cells[row, headers["description"]].Value?.ToString()?.Trim()
                                    : null;
                                var image = headers.ContainsKey("image")
                                    ? worksheet.Cells[row, headers["image"]].Value?.ToString()?.Trim()
                                    : null;
                                var link = headers.ContainsKey("link")
                                    ? worksheet.Cells[row, headers["link"]].Value?.ToString()?.Trim()
                                    : null;
                                var isActive = headers.ContainsKey("isactive") || headers.ContainsKey("aktif")
                                    ? ParseBool(worksheet.Cells[row, headers.ContainsKey("isactive") ? headers["isactive"] : headers["aktif"]].Value?.ToString() ?? "true")
                                    : true;

                                int? orderNumber = null;
                                if (headers.ContainsKey("sira_numarasi"))
                                {
                                    var orderCell = worksheet.Cells[row, headers["sira_numarasi"]].Value;
                                    if (orderCell != null)
                                    {
                                        try { orderNumber = Convert.ToInt32(orderCell); }
                                        catch
                                        {
                                            if (int.TryParse(orderCell.ToString()?.Trim(), out int parsed))
                                                orderNumber = parsed;
                                        }
                                    }
                                }

                                // Mevcut kaydı bul → varsa güncelle, yoksa ekle
                                var existing = slidersToAdd.FirstOrDefault(s => s.Title == title)
                                               ?? _context.Sliders.FirstOrDefault(s => s.Title == title);

                                if (existing != null && existing.Id > 0)
                                {
                                    existing.Description = description;
                                    if (!string.IsNullOrEmpty(image)) existing.Image = image;
                                    existing.Link = link;
                                    existing.IsActive = isActive;
                                    if (orderNumber.HasValue) existing.OrderNumber = orderNumber;
                                    existing.UpdatedDate = DateTime.Now;
                                    slidersToUpdate.Add(existing);
                                    updateCount++;
                                }
                                else if (existing == null)
                                {
                                    slidersToAdd.Add(new Slider
                                    {
                                        Title = title,
                                        Description = description,
                                        Image = image,
                                        Link = link,
                                        IsActive = isActive,
                                        OrderNumber = orderNumber
                                    });
                                    successCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Satır {row}: {ex.Message}");
                                continue;
                            }
                        }

                        if (slidersToAdd.Any())
                        {
                            _context.Sliders.AddRange(slidersToAdd);
                            await _context.SaveChangesAsync();
                        }
                        if (slidersToUpdate.Any())
                        {
                            _context.Sliders.UpdateRange(slidersToUpdate);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Insert(0, $"Genel hata: {ex.Message}");
                return (0, 0, errors);
            }

            return (successCount, updateCount, errors);
        }

        /// <summary>
        /// Excel dosyasından Products verilerini içeri aktar
        /// Beklenen sütunlar: urun_adi (zorunlu), aciklama, urun_kodu, fiyat, stok, kategori, marka, durum, anasayfa
        /// </summary>
        public async Task<(int successCount, int updateCount, List<string> errors)> ImportProductsAsync(IFormFile file)
        {
            var errors = new List<string>();
            int successCount = 0;
            int updateCount = 0;
            var productsToAdd = new List<Core.Entities.Product>();
            var productsToUpdate = new List<Core.Entities.Product>();

            try
            {
                var categories = _context.Categories.ToList();
                var brands = _context.Brands.ToList();

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new OfficeOpenXml.ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            errors.Add("Excel dosyasında çalışma sayfası bulunamadı.");
                            return (0, 0, errors);
                        }

                        int rowCount = worksheet.Dimension?.Rows ?? 0;
                        if (rowCount < 2)
                        {
                            errors.Add("Excel dosyasında veri bulunamadı. Başlığından sonra en az 1 satır verisi olmalı.");
                            return (0, 0, errors);
                        }

                        var headers = new Dictionary<string, int>();
                        for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                        {
                            var headerValue = worksheet.Cells[1, col].Value?.ToString()?.ToLower().Trim();
                            if (!string.IsNullOrEmpty(headerValue))
                                headers[headerValue] = col;
                        }

                        if (!headers.ContainsKey("urun_adi"))
                        {
                            errors.Add("Excel dosyasında 'urun_adi' sütunu bulunamadı.");
                            return (0, 0, errors);
                        }

                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                var title = worksheet.Cells[row, headers["urun_adi"]].Value?.ToString()?.Trim();
                                if (string.IsNullOrEmpty(title))
                                {
                                    errors.Add($"Satır {row}: Ürün adı boş olamaz.");
                                    continue;
                                }

                                var description = headers.ContainsKey("aciklama")
                                    ? worksheet.Cells[row, headers["aciklama"]].Value?.ToString()?.Trim()
                                    : null;

                                var productCode = headers.ContainsKey("urun_kodu")
                                    ? worksheet.Cells[row, headers["urun_kodu"]].Value?.ToString()?.Trim()
                                    : null;

                                decimal price = 0;
                                if (headers.ContainsKey("fiyat"))
                                {
                                    var priceCell = worksheet.Cells[row, headers["fiyat"]].Value;
                                    if (priceCell != null)
                                    {
                                        try { price = Convert.ToDecimal(priceCell); }
                                        catch { decimal.TryParse(priceCell.ToString()?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out price); }
                                    }
                                }

                                int stockCount = 0;
                                if (headers.ContainsKey("stok"))
                                {
                                    var stockCell = worksheet.Cells[row, headers["stok"]].Value;
                                    if (stockCell != null)
                                    {
                                        try { stockCount = Convert.ToInt32(stockCell); }
                                        catch { int.TryParse(stockCell.ToString()?.Trim(), out stockCount); }
                                    }
                                }

                                int? categoryId = null;
                                if (headers.ContainsKey("kategori"))
                                {
                                    var catName = worksheet.Cells[row, headers["kategori"]].Value?.ToString()?.Trim();
                                    if (!string.IsNullOrEmpty(catName))
                                    {
                                        var cat = categories.FirstOrDefault(c => c.Title.Equals(catName, StringComparison.OrdinalIgnoreCase));
                                        if (cat != null) categoryId = cat.Id;
                                        else errors.Add($"Satır {row}: '{catName}' kategorisi bulunamadı, kategori atanmadı.");
                                    }
                                }

                                int? brandId = null;
                                if (headers.ContainsKey("marka"))
                                {
                                    var brandName = worksheet.Cells[row, headers["marka"]].Value?.ToString()?.Trim();
                                    if (!string.IsNullOrEmpty(brandName))
                                    {
                                        var brand = brands.FirstOrDefault(b => b.Name.Equals(brandName, StringComparison.OrdinalIgnoreCase));
                                        if (brand != null) brandId = brand.Id;
                                        else errors.Add($"Satır {row}: '{brandName}' markası bulunamadı, marka atanmadı.");
                                    }
                                }

                                var isActive = headers.ContainsKey("durum")
                                    ? ParseBool(worksheet.Cells[row, headers["durum"]].Value?.ToString() ?? "true")
                                    : true;

                                var isHome = headers.ContainsKey("anasayfa")
                                    ? ParseBool(worksheet.Cells[row, headers["anasayfa"]].Value?.ToString() ?? "false")
                                    : false;

                                int? orderNumber = null;
                                if (headers.ContainsKey("sira_numarasi"))
                                {
                                    var orderCell = worksheet.Cells[row, headers["sira_numarasi"]].Value;
                                    if (orderCell != null)
                                    {
                                        try { orderNumber = Convert.ToInt32(orderCell); }
                                        catch
                                        {
                                            if (int.TryParse(orderCell.ToString()?.Trim(), out int parsed))
                                                orderNumber = parsed;
                                        }
                                    }
                                }

                                Core.Entities.Product? existing = null;
                                if (!string.IsNullOrEmpty(productCode))
                                    existing = _context.Products.FirstOrDefault(p => p.ProductCode == productCode);
                                if (existing == null)
                                    existing = productsToAdd.FirstOrDefault(p => p.Title == title)
                                               ?? _context.Products.FirstOrDefault(p => p.Title == title);

                                if (existing != null && existing.Id > 0)
                                {
                                    existing.Title = title;
                                    existing.Description = description;
                                    if (!string.IsNullOrEmpty(productCode)) existing.ProductCode = productCode;
                                    existing.Price = price;
                                    existing.StockCount = stockCount;
                                    if (categoryId.HasValue) existing.CategoryId = categoryId;
                                    if (brandId.HasValue) existing.BrandId = brandId;
                                    existing.IsActive = isActive;
                                    existing.IsHome = isHome;
                                    if (orderNumber.HasValue) existing.OrderNumber = orderNumber;
                                    existing.UpdatedDate = DateTime.Now;
                                    productsToUpdate.Add(existing);
                                    updateCount++;
                                }
                                else if (existing == null)
                                {
                                    productsToAdd.Add(new Core.Entities.Product
                                    {
                                        Title = title,
                                        Description = description,
                                        ProductCode = productCode,
                                        Price = price,
                                        StockCount = stockCount,
                                        CategoryId = categoryId,
                                        BrandId = brandId,
                                        IsActive = isActive,
                                        IsHome = isHome,
                                        OrderNumber = orderNumber
                                    });
                                    successCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Satır {row}: {ex.Message}");
                            }
                        }

                        int totalToProcess = productsToAdd.Count + productsToUpdate.Count;
                        int processed = 0;
                        int batchSize = 100;

                        for (int i = 0; i < productsToAdd.Count; i += batchSize)
                        {
                            var batch = productsToAdd.Skip(i).Take(batchSize).ToList();
                            _context.Products.AddRange(batch);
                            await _context.SaveChangesAsync();
                            processed += batch.Count;
                            var percentage = totalToProcess > 0 ? (int)((processed * 100) / totalToProcess) : 100;
                            await _hubContext.Clients.All.SendAsync("ReceiveProgress",
                                new { Processed = processed, Total = totalToProcess, Percentage = percentage, Message = $"{processed} / {totalToProcess} ürün işlendi" });
                        }

                        for (int i = 0; i < productsToUpdate.Count; i += batchSize)
                        {
                            var batch = productsToUpdate.Skip(i).Take(batchSize).ToList();
                            _context.Products.UpdateRange(batch);
                            await _context.SaveChangesAsync();
                            processed += batch.Count;
                            var percentage = totalToProcess > 0 ? (int)((processed * 100) / totalToProcess) : 100;
                            await _hubContext.Clients.All.SendAsync("ReceiveProgress",
                                new { Processed = processed, Total = totalToProcess, Percentage = percentage, Message = $"{processed} / {totalToProcess} ürün işlendi" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Insert(0, $"Genel hata: {ex.Message}");
                await _hubContext.Clients.All.SendAsync("ReceiveError", ex.Message);
                return (0, 0, errors);
            }

            await _hubContext.Clients.All.SendAsync("ReceiveComplete",
                new { SuccessCount = successCount, UpdateCount = updateCount, Errors = errors, TotalErrors = errors.Count });

            return (successCount, updateCount, errors);
        }

        private bool ParseBool(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            return value.Equals("true", StringComparison.OrdinalIgnoreCase) || 
                   value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("evet", StringComparison.OrdinalIgnoreCase);
        }
    }
}
