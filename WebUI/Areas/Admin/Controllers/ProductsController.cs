using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Data.Context;
using WebUI.Helper;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using WebUI.Areas.Admin.Models;
using Service.Extensions;

namespace WebUI.Areas.Admin.Controllers
{
    public class ProductsController : AdminBaseController
    {
        private readonly DatabaseContext _context;
        private readonly ExcelImportHelper _excelHelper;
        private readonly FileHelper _fileHelper;
        private readonly string _uploadPath;
        private readonly IWebHostEnvironment _env;

        public ProductsController(DatabaseContext context, ExcelImportHelper excelHelper, FileHelper fileHelper,
            IWebHostEnvironment env)
        {
            _context = context;
            _excelHelper = excelHelper;
            _fileHelper = fileHelper;
            _env = env;
            _uploadPath = Path.Combine(_env.WebRootPath, "uploads", "products");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            List<Product>? data = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            List<StartCardModel> startCards = new()
            {
                new StartCardModel
                {
                    Title = "Tüm Ürünler",
                    Value = data.Count,
                    Class = "info",
                    Tooltip = "Sitede bulunan toplam ürün sayısı",
                    Icon = "fa-solid fa-boxes-stacked"
                },
                new StartCardModel
                {
                    Title = "Aktif Ürün",
                    Value = data.Count(p => FunctionHelper.IsActive(p.Status)),
                    Class = "success",
                    Tooltip = "Sitede aktif olarak satışta olan ürün sayısı",
                    Icon = "fa-solid fa-check"
                },
                new StartCardModel
                {
                    Title = "Kritik Stok",
                    Value = data.Count(p => p.StockCount < 10),
                    Class = "danger",
                    Tooltip = "Stok adedi 10'un altında olan ürün sayısı",
                    Icon = "fa-solid fa-triangle-exclamation"
                },
                new StartCardModel
                {
                    Title = "Taslak Ürünler",
                    Value = data.Count(p => FunctionHelper.IsDraft(p.Status)),
                    Class = "secondary",
                    Tooltip = "Sitede taslak durumda olan ürün sayısı",
                    Icon = "fa-solid fa-file"
                }
            };

            List<BreadcrumbItem> breadcrumb = new()
            {
                new BreadcrumbItem { Title = "Ürünler" }
            };

            ViewBag.Breadcrumbs = breadcrumb;
            ViewBag.StartCards = startCards;

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
                return NotFound();

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Ürünler", Controller= "Products", Action = "Index" },
                new BreadcrumbItem { Title = product.Title}
            };

            ViewBag.Breadcrumbs = breadcrumbs;

            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> Form(int? id)
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Brands = await _context.Brands.ToListAsync();

            Product? data = new();

            if (id.HasValue)
            {
                data = await _context.Products.FindAsync(id);

                if (data == null)
                    return NotFound();
            }

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Ürünler", Controller= "Products", Action = "Index" },
                new BreadcrumbItem { Title = data?.Title ?? "Yeni Ürün" }
            };

            ViewBag.Breadcrumbs = breadcrumbs;

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, IFormFile? Image, string? ImageUrl)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _context.Categories.ToListAsync();
                ViewBag.Brands = await _context.Brands.ToListAsync();
                return View("Form", model);
            }

            var (imagePath, imageFailed) = await ResolveImageAsync(Image, ImageUrl, _fileHelper, _uploadPath);
            if (imageFailed)
            {
                SetSweetAlertMessage("Hata",
                    "Dosya yüklenirken hata oluştu. Lütfen geçerli bir görüntü dosyası seçin (JPG, PNG, GIF, WEBP).",
                    "error");
                ViewBag.Categories = await _context.Categories.ToListAsync();
                ViewBag.Brands = await _context.Brands.ToListAsync();
                return View("Form", model);
            }

            if (imagePath != null) model.Image = imagePath;

            _context.Products.Add(model);
            await _context.SaveChangesAsync();
            SetSweetAlertMessage("Başarılı", "Ürün başarıyla oluşturuldu.", "success");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product model, IFormFile? Image, string? ImageUrl,
            bool DeleteImage = false)
        {
            if (id != model.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _context.Categories.ToListAsync();
                ViewBag.Brands = await _context.Brands.ToListAsync();
                return View("Form", model);
            }

            try
            {
                var (imagePath, imageFailed) = await ResolveImageOnEditAsync(Image, ImageUrl, model.Image, DeleteImage,
                    _fileHelper, _uploadPath);
                if (imageFailed)
                {
                    SetSweetAlertMessage("Hata",
                        "Dosya yüklenirken hata oluştu. Lütfen geçerli bir görüntü dosyası seçin (JPG, PNG, GIF, WEBP).",
                        "error");
                    ViewBag.Categories = await _context.Categories.ToListAsync();
                    ViewBag.Brands = await _context.Brands.ToListAsync();
                    return View("Form", model);
                }

                model.Image = imagePath;

                _context.Products.Update(model);
                await _context.SaveChangesAsync();
                SetSweetAlertMessage("Başarılı", "Ürün başarıyla güncellendi.", "success");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Products.Any(e => e.Id == model.Id))
                    return NotFound();
                else
                    throw;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            try
            {
                if (!string.IsNullOrEmpty(product.Image))
                {
                    _fileHelper.Delete(product.Image);
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                SetSweetAlertMessage("Başarılı", "Ürün başarıyla silindi.", "success");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                SetSweetAlertMessage("Hata", $"Silme işlemi başarısız: {ex.Message}", "error");
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                SetSweetAlertMessage("Hata", "Silinecek kayıt seçilmedi.", "error");
                return RedirectToAction(nameof(Index));
            }

            var items = await _context.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
            foreach (var item in items)
                if (!string.IsNullOrEmpty(item.Image))
                    _fileHelper.Delete(item.Image);

            _context.Products.RemoveRange(items);
            await _context.SaveChangesAsync();

            SetSweetAlertMessage("Başarılı", $"{items.Count} ürün silindi.", "success");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public Task<IActionResult> ImportExcel(IFormFile file)
            => ExcelImport(file, _excelHelper.ImportProductsAsync,
                (s, u) => $"{s} ürün eklendi, {u} ürün güncellendi.");

        [HttpGet]
        public IActionResult DownloadSampleExcel()
            => DownloadSampleExcel("urunler_ornek.xlsx",
                new[]
                {
                    (DisplayName<Product>(nameof(Product.Title)), true),
                    (DisplayName<Product>(nameof(Product.Description)), false),
                    (DisplayName<Product>(nameof(Product.ProductCode)), false),
                    (DisplayName<Product>(nameof(Product.Image)), false),
                    (DisplayName<Product>(nameof(Product.Price)), false),
                    (DisplayName<Product>(nameof(Product.StockCount)), false),
                    (DisplayName<Product>(nameof(Product.CategoryId)), false),
                    (DisplayName<Product>(nameof(Product.BrandId)), false),
                    (DisplayName<Product>(nameof(Product.Status)), false),
                    (DisplayName<Product>(nameof(Product.IsHome)), false),
                    (DisplayName<Product>(nameof(Product.OrderNumber)), false)
                },
                new object[]
                {
                    "Örnek Ürün", "Kaliteli bir ürün", "", "SKU-001", 99.90m, 50, "Örnek Kategori", "Örnek Marka Aş",
                    1, "hayır", 1
                });

        [HttpGet]
        public async Task<IActionResult> ExportExcel()
        {
            var products = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Ürünler");

            ws.Cells[1, 1].Value = DisplayName<Product>(nameof(Product.Title));
            ws.Cells[1, 2].Value = DisplayName<Product>(nameof(Product.Description));
            ws.Cells[1, 3].Value = DisplayName<Product>(nameof(Product.ProductCode));
            ws.Cells[1, 4].Value = DisplayName<Product>(nameof(Product.Image));
            ws.Cells[1, 5].Value = DisplayName<Product>(nameof(Product.Price));
            ws.Cells[1, 6].Value = DisplayName<Product>(nameof(Product.StockCount));
            ws.Cells[1, 7].Value = DisplayName<Product>(nameof(Product.CategoryId));
            ws.Cells[1, 8].Value = DisplayName<Product>(nameof(Product.BrandId));
            ws.Cells[1, 9].Value = DisplayName<Product>(nameof(Product.Status));
            ws.Cells[1, 10].Value = DisplayName<Product>(nameof(Product.IsHome));
            ws.Cells[1, 11].Value = DisplayName<Product>(nameof(Product.OrderNumber));

            using (var headerRange = ws.Cells[1, 1, 1, 11])
            {
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0x4E, 0x73, 0xDF));
                headerRange.Style.Font.Color.SetColor(Color.White);
            }

            int row = 2;
            foreach (var p in products)
            {
                ws.Cells[row, 1].Value = p.Title;
                ws.Cells[row, 2].Value = p.Description;
                ws.Cells[row, 3].Value = p.ProductCode;
                ws.Cells[row, 4].Value = p.Image;
                ws.Cells[row, 5].Value = p.Price;
                ws.Cells[row, 6].Value = p.StockCount;
                ws.Cells[row, 7].Value = p.Category?.Title;
                ws.Cells[row, 8].Value = p.Brand?.Name;
                ws.Cells[row, 9].Value = p.Status;
                ws.Cells[row, 10].Value = p.IsHome ? "evet" : "hayir";
                ws.Cells[row, 11].Value = p.OrderNumber;
                row++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            var bytes = package.GetAsByteArray();
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"urunler_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }
    }
}
