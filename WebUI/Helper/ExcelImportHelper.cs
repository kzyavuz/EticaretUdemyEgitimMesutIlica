using Core.Entities;
using Core.Enum;
using Data.Context;
using Microsoft.AspNetCore.SignalR;
using OfficeOpenXml;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
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
                            var headerValue = worksheet.Cells[1, col].Value?.ToString()?.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")).Trim();
                            if (!string.IsNullOrEmpty(headerValue))
                            {
                                headers[headerValue] = col;
                            }
                        }

                        // Sütun başlıklarını entity Display adlarından türet
                        var trCulture    = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
                        var kName        = GetDisplayName<Brand>(nameof(Brand.Name)).ToLower(trCulture);
                        var kDescription = GetDisplayName<Brand>(nameof(Brand.Description)).ToLower(trCulture);
                        var kLogo        = GetDisplayName<Brand>(nameof(Brand.Logo)).ToLower(trCulture);
                        var kIsActive    = GetDisplayName<Brand>(nameof(Brand.Status)).ToLower(trCulture);
                        var kOrderNumber = GetDisplayName<Brand>(nameof(Brand.OrderNumber)).ToLower(trCulture);

                        // Gerekli sütunları kontrol et — başlıklar örnek Excel ile birebir eşleşmeli
                        if (!headers.ContainsKey(kName))
                        {
                            errors.Add($"Excel dosyasında zorunlu '{GetDisplayName<Brand>(nameof(Brand.Name))}' sütunu bulunamadı.");
                            return (0, 0, errors);
                        }

                        var processedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        // headers küçük harfe çevrildiği için karşılaştırmalar lowercase
                        var descCol    = headers.ContainsKey(kDescription) ? kDescription : null;
                        var logoCol    = headers.ContainsKey(kLogo) ? kLogo : null;
                        var activeCol  = headers.ContainsKey(kIsActive) ? kIsActive : null;
                        var orderNumberCol = headers.ContainsKey(kOrderNumber) ? kOrderNumber : null;

                        // Verileri oku (Row 2 başlayarak)
                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                var name = worksheet.Cells[row, headers[kName]].Value?.ToString()?.Trim();

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
                                    existingBrand.Status = isActive ? DataStatus.Active : DataStatus.Draft;
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
                                        Status = isActive ? DataStatus.Active : DataStatus.Draft,
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
                            var headerValue = worksheet.Cells[1, col].Value?.ToString()?.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")).Trim();
                            if (!string.IsNullOrEmpty(headerValue))
                            {
                                headers[headerValue] = col;
                            }
                        }

                        // Sütun başlıklarını entity Display adlarından türet
                        var trCulture    = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
                        var kTitle       = GetDisplayName<Category>(nameof(Category.Title)).ToLower(trCulture);
                        var kDescription = GetDisplayName<Category>(nameof(Category.Description)).ToLower(trCulture);
                        var kImage       = GetDisplayName<Category>(nameof(Category.Image)).ToLower(trCulture);
                        var kParentId    = GetDisplayName<Category>(nameof(Category.ParentId)).ToLower(trCulture);
                        var kIsTopMenu   = GetDisplayName<Category>(nameof(Category.ISTopMenu)).ToLower(trCulture);
                        var kIsActive    = GetDisplayName<Category>(nameof(Category.Status)).ToLower(trCulture);
                        var kOrderNumber = GetDisplayName<Category>(nameof(Category.OrderNumber)).ToLower(trCulture);

                        // Gerekli sütunları kontrol et — başlıklar örnek Excel ile birebir eşleşmeli
                        if (!headers.ContainsKey(kTitle))
                        {
                            errors.Add($"Excel dosyasında zorunlu '{GetDisplayName<Category>(nameof(Category.Title))}' sütunu bulunamadı.");
                            return (0, 0, errors);
                        }

                        var allCategories = _context.Categories.ToList();

                        // Verileri oku (Row 2 başlayarak)
                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                var title = worksheet.Cells[row, headers[kTitle]].Value?.ToString()?.Trim();

                                if (string.IsNullOrEmpty(title))
                                {
                                    errors.Add($"Satır {row}: Kategori başlığı boş olamaz.");
                                    continue;
                                }

                                var description = headers.ContainsKey(kDescription)
                                    ? worksheet.Cells[row, headers[kDescription]].Value?.ToString()?.Trim()
                                    : null;
                                var image = headers.ContainsKey(kImage)
                                    ? worksheet.Cells[row, headers[kImage]].Value?.ToString()?.Trim()
                                    : null;

                                int parentId = 0;
                                if (headers.ContainsKey(kParentId))
                                {
                                    var parentName = worksheet.Cells[row, headers[kParentId]].Value?.ToString()?.Trim();
                                    if (!string.IsNullOrEmpty(parentName))
                                    {
                                        var parentCat = allCategories.FirstOrDefault(c => c.Title.Equals(parentName, StringComparison.OrdinalIgnoreCase))
                                                        ?? categoriesToAdd.FirstOrDefault(c => c.Title.Equals(parentName, StringComparison.OrdinalIgnoreCase));
                                        if (parentCat != null)
                                            parentId = parentCat.Id;
                                        else
                                            errors.Add($"Satır {row}: '{parentName}' üst kategorisi bulunamadı, üst kategori atanmadı.");
                                    }
                                }

                                var isTopMenu = headers.ContainsKey(kIsTopMenu)
                                    ? ParseBool(worksheet.Cells[row, headers[kIsTopMenu]].Value?.ToString() ?? "false")
                                    : false;
                                var isActive = headers.ContainsKey(kIsActive)
                                    ? ParseBool(worksheet.Cells[row, headers[kIsActive]].Value?.ToString() ?? "true")
                                    : true;

                                int? orderNumber = null;
                                if (headers.ContainsKey(kOrderNumber))
                                {
                                    var orderCell = worksheet.Cells[row, headers[kOrderNumber]].Value;
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
                                    existing.ParentId = parentId;
                                    existing.ISTopMenu = isTopMenu;
                                    existing.Status = isActive ? DataStatus.Active : DataStatus.Draft;
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
                                        ParentId = parentId,
                                        ISTopMenu = isTopMenu,
                                        Status = isActive ? DataStatus.Active : DataStatus.Draft,
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
                            var headerValue = worksheet.Cells[1, col].Value?.ToString()?.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")).Trim();
                            if (!string.IsNullOrEmpty(headerValue))
                            {
                                headers[headerValue] = col;
                            }
                        }

                        // Sütun başlıklarını entity Display adlarından türet
                        var trCulture    = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
                        var kTitle       = GetDisplayName<News>(nameof(News.Title)).ToLower(trCulture);
                        var kDescription = GetDisplayName<News>(nameof(News.Description)).ToLower(trCulture);
                        var kImage       = GetDisplayName<News>(nameof(News.Image)).ToLower(trCulture);
                        var kIsActive    = GetDisplayName<News>(nameof(News.Status)).ToLower(trCulture);
                        var kOrderNumber = GetDisplayName<News>(nameof(News.OrderNumber)).ToLower(trCulture);

                        // Gerekli sütunları kontrol et — başlıklar örnek Excel ile birebir eşleşmeli
                        if (!headers.ContainsKey(kTitle))
                        {
                            errors.Add($"Excel dosyasında zorunlu '{GetDisplayName<News>(nameof(News.Title))}' sütunu bulunamadı.");
                            return (0, 0, errors);
                        }

                        // Verileri oku (Row 2 başlayarak)
                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                var title = worksheet.Cells[row, headers[kTitle]].Value?.ToString()?.Trim();

                                if (string.IsNullOrEmpty(title))
                                {
                                    errors.Add($"Satır {row}: Kampanya başlığı boş olamaz.");
                                    continue;
                                }

                                var description = headers.ContainsKey(kDescription)
                                    ? worksheet.Cells[row, headers[kDescription]].Value?.ToString()?.Trim()
                                    : null;
                                var image = headers.ContainsKey(kImage)
                                    ? worksheet.Cells[row, headers[kImage]].Value?.ToString()?.Trim()
                                    : null;
                                var isActive = headers.ContainsKey(kIsActive)
                                    ? ParseBool(worksheet.Cells[row, headers[kIsActive]].Value?.ToString() ?? "true")
                                    : true;

                                int? orderNumber = null;
                                if (headers.ContainsKey(kOrderNumber))
                                {
                                    var orderCell = worksheet.Cells[row, headers[kOrderNumber]].Value;
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
                                    existing.Status = isActive ? DataStatus.Active : DataStatus.Draft;
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
                                        Status = isActive ? DataStatus.Active : DataStatus.Draft,
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
                            var headerValue = worksheet.Cells[1, col].Value?.ToString()?.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")).Trim();
                            if (!string.IsNullOrEmpty(headerValue))
                            {
                                headers[headerValue] = col;
                            }
                        }

                        // Sütun başlıklarını entity Display adlarından türet
                        var trCulture    = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
                        var kTitle       = GetDisplayName<Slider>(nameof(Slider.Title)).ToLower(trCulture);
                        var kDescription = GetDisplayName<Slider>(nameof(Slider.Description)).ToLower(trCulture);
                        var kImage       = GetDisplayName<Slider>(nameof(Slider.Image)).ToLower(trCulture);
                        const string kLink = "bağlantı"; // Slider'da bağlantı alanı Slug'a eşlenir
                        var kIsActive    = GetDisplayName<Slider>(nameof(Slider.Status)).ToLower(trCulture);
                        var kOrderNumber = GetDisplayName<Slider>(nameof(Slider.OrderNumber)).ToLower(trCulture);

                        // Gerekli sütunları kontrol et — başlıklar örnek Excel ile birebir eşleşmeli
                        if (!headers.ContainsKey(kTitle))
                        {
                            errors.Add($"Excel dosyasında zorunlu '{GetDisplayName<Slider>(nameof(Slider.Title))}' sütunu bulunamadı.");
                            return (0, 0, errors);
                        }

                        // Verileri oku (Row 2 başlayarak)
                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                var title = worksheet.Cells[row, headers[kTitle]].Value?.ToString()?.Trim();

                                if (string.IsNullOrEmpty(title))
                                {
                                    errors.Add($"Satır {row}: Slider başlığı boş olamaz.");
                                    continue;
                                }

                                var description = headers.ContainsKey(kDescription)
                                    ? worksheet.Cells[row, headers[kDescription]].Value?.ToString()?.Trim()
                                    : null;
                                var image = headers.ContainsKey(kImage)
                                    ? worksheet.Cells[row, headers[kImage]].Value?.ToString()?.Trim()
                                    : null;
                                var link = headers.ContainsKey(kLink)
                                    ? worksheet.Cells[row, headers[kLink]].Value?.ToString()?.Trim()
                                    : null;
                                var isActive = headers.ContainsKey(kIsActive)
                                    ? ParseBool(worksheet.Cells[row, headers[kIsActive]].Value?.ToString() ?? "true")
                                    : true;

                                int? orderNumber = null;
                                if (headers.ContainsKey(kOrderNumber))
                                {
                                    var orderCell = worksheet.Cells[row, headers[kOrderNumber]].Value;
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
                                    existing.Slug = link;
                                    existing.Status = isActive ? DataStatus.Active : DataStatus.Draft;
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
                                        Slug = link,
                                        Status = isActive ? DataStatus.Active : DataStatus.Draft,
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
            var productsToAdd = new List<Product>();
            var productsToUpdate = new List<Product>();

            try
            {
                var categories = _context.Categories.ToList();
                var brands = _context.Brands.ToList();

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

                        var headers = new Dictionary<string, int>();
                        for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                        {
                            var headerValue = worksheet.Cells[1, col].Value?.ToString()?.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")).Trim();
                            if (!string.IsNullOrEmpty(headerValue))
                                headers[headerValue] = col;
                        }

                        // Sütun başlıklarını entity Display adlarından türet
                        var trCulture    = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
                        var kTitle       = GetDisplayName<Product>(nameof(Product.Title)).ToLower(trCulture);
                        var kDescription = GetDisplayName<Product>(nameof(Product.Description)).ToLower(trCulture);
                        var kImage       = GetDisplayName<Product>(nameof(Product.Image)).ToLower(trCulture);
                        var kProductCode = GetDisplayName<Product>(nameof(Product.ProductCode)).ToLower(trCulture);
                        var kPrice       = GetDisplayName<Product>(nameof(Product.Price)).ToLower(trCulture);
                        var kStockCount  = GetDisplayName<Product>(nameof(Product.StockCount)).ToLower(trCulture);
                        var kCategoryId  = GetDisplayName<Product>(nameof(Product.CategoryId)).ToLower(trCulture);
                        var kBrandId     = GetDisplayName<Product>(nameof(Product.BrandId)).ToLower(trCulture);
                        var kIsActive    = GetDisplayName<Product>(nameof(Product.Status)).ToLower(trCulture);
                        var kIsHome      = GetDisplayName<Product>(nameof(Product.IsHome)).ToLower(trCulture);
                        var kOrderNumber = GetDisplayName<Product>(nameof(Product.OrderNumber)).ToLower(trCulture);

                        // Gerekli sütunları kontrol et — başlıklar örnek Excel ile birebir eşleşmeli
                        if (!headers.ContainsKey(kTitle))
                        {
                            errors.Add($"Excel dosyasında zorunlu '{GetDisplayName<Product>(nameof(Product.Title))}' sütunu bulunamadı.");
                            return (0, 0, errors);
                        }

                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                var title = worksheet.Cells[row, headers[kTitle]].Value?.ToString()?.Trim();
                                if (string.IsNullOrEmpty(title))
                                {
                                    errors.Add($"Satır {row}: Ürün adı boş olamaz.");
                                    continue;
                                }

                                var description = headers.ContainsKey(kDescription)
                                    ? worksheet.Cells[row, headers[kDescription]].Value?.ToString()?.Trim()
                                    : null;

                                var image = headers.ContainsKey(kImage)
                                    ? worksheet.Cells[row, headers[kImage]].Value?.ToString()?.Trim()
                                    : null;

                                var productCode = headers.ContainsKey(kProductCode)
                                    ? worksheet.Cells[row, headers[kProductCode]].Value?.ToString()?.Trim()
                                    : null;

                                decimal price = 0;
                                if (headers.ContainsKey(kPrice))
                                {
                                    var priceCell = worksheet.Cells[row, headers[kPrice]].Value;
                                    if (priceCell != null)
                                    {
                                        try { price = Convert.ToDecimal(priceCell); }
                                        catch { decimal.TryParse(priceCell.ToString()?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out price); }
                                    }
                                }

                                int stockCount = 0;
                                if (headers.ContainsKey(kStockCount))
                                {
                                    var stockCell = worksheet.Cells[row, headers[kStockCount]].Value;
                                    if (stockCell != null)
                                    {
                                        try { stockCount = Convert.ToInt32(stockCell); }
                                        catch { int.TryParse(stockCell.ToString()?.Trim(), out stockCount); }
                                    }
                                }

                                int? categoryId = null;
                                if (headers.ContainsKey(kCategoryId))
                                {
                                    var catName = worksheet.Cells[row, headers[kCategoryId]].Value?.ToString()?.Trim();
                                    if (!string.IsNullOrEmpty(catName))
                                    {
                                        var cat = categories.FirstOrDefault(c => c.Title.Equals(catName, StringComparison.OrdinalIgnoreCase));
                                        if (cat != null) categoryId = cat.Id;
                                        else errors.Add($"Satır {row}: '{catName}' kategorisi bulunamadı, kategori atanmadı.");
                                    }
                                }

                                int? brandId = null;
                                if (headers.ContainsKey(kBrandId))
                                {
                                    var brandName = worksheet.Cells[row, headers[kBrandId]].Value?.ToString()?.Trim();
                                    if (!string.IsNullOrEmpty(brandName))
                                    {
                                        var brand = brands.FirstOrDefault(b => b.Name.Equals(brandName, StringComparison.OrdinalIgnoreCase));
                                        if (brand != null) brandId = brand.Id;
                                        else errors.Add($"Satır {row}: '{brandName}' markası bulunamadı, marka atanmadı.");
                                    }
                                }

                                var isActive = headers.ContainsKey(kIsActive)
                                    ? ParseBool(worksheet.Cells[row, headers[kIsActive]].Value?.ToString() ?? "true")
                                    : true;

                                var isHome = headers.ContainsKey(kIsHome)
                                    ? ParseBool(worksheet.Cells[row, headers[kIsHome]].Value?.ToString() ?? "false")
                                    : false;

                                int? orderNumber = null;
                                if (headers.ContainsKey(kOrderNumber))
                                {
                                    var orderCell = worksheet.Cells[row, headers[kOrderNumber]].Value;
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

                                Product? existing = null;
                                if (!string.IsNullOrEmpty(productCode))
                                    existing = _context.Products.FirstOrDefault(p => p.ProductCode == productCode);
                                if (existing == null)
                                    existing = productsToAdd.FirstOrDefault(p => p.Title == title)
                                               ?? _context.Products.FirstOrDefault(p => p.Title == title);

                                if (existing != null && existing.Id > 0)
                                {
                                    existing.Title = title;
                                    existing.Description = description;
                                    if (!string.IsNullOrEmpty(image)) existing.Image = image;
                                    if (!string.IsNullOrEmpty(productCode)) existing.ProductCode = productCode;
                                    existing.Price = price;
                                    existing.StockCount = stockCount;
                                    if (categoryId.HasValue) existing.CategoryId = categoryId;
                                    if (brandId.HasValue) existing.BrandId = brandId;
                                    existing.Status = isActive ? DataStatus.Active : DataStatus.Draft;
                                    existing.IsHome = isHome;
                                    if (orderNumber.HasValue) existing.OrderNumber = orderNumber;
                                    existing.UpdatedDate = DateTime.Now;
                                    productsToUpdate.Add(existing);
                                    updateCount++;
                                }
                                else if (existing == null)
                                {
                                    productsToAdd.Add(new Product
                                    {
                                        Title = title,
                                        Description = description,
                                        Image = image,
                                        ProductCode = productCode,
                                        Price = price,
                                        StockCount = stockCount,
                                        CategoryId = categoryId,
                                        BrandId = brandId,
                                        Status = isActive ? DataStatus.Active : DataStatus.Draft,
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

        private static string GetDisplayName<T>(string propertyName)
        {
            return typeof(T)
                .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
                ?.GetCustomAttribute<DisplayAttribute>()
                ?.Name ?? propertyName;
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
