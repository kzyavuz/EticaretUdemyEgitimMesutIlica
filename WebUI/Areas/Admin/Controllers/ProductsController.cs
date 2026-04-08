using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Data.Context;
using WebUI.Helper;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductsController : AdminBaseController
    {
        private readonly DatabaseContext _context;
        private readonly ExcelImportHelper _excelHelper;
        private readonly FileHelper _fileHelper;
        private readonly string _uploadPath;
        private readonly IWebHostEnvironment _env;

        public ProductsController(DatabaseContext context, ExcelImportHelper excelHelper, FileHelper fileHelper, IWebHostEnvironment env)
        {
            _context = context;
            _excelHelper = excelHelper;
            _fileHelper = fileHelper;
            _env = env;
            _uploadPath = Path.Combine(_env.WebRootPath, "uploads", "products");
        }

        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (product == null)
                return NotFound();

            return View(product);
        }

        public async Task<IActionResult> Form(int? id)
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Brands = await _context.Brands.ToListAsync();

            if (id.HasValue)
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFound();
                return View(product);
            }

            return View(new Product());
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
                SetSweetAlertMessage("error", "Hata", "Dosya yüklenirken hata oluştu. Lütfen geçerli bir görüntü dosyası seçin (JPG, PNG, GIF, WEBP).");
                ViewBag.Categories = await _context.Categories.ToListAsync();
                ViewBag.Brands = await _context.Brands.ToListAsync();
                return View("Form", model);
            }
            if (imagePath != null) model.Image = imagePath;

            _context.Products.Add(model);
            await _context.SaveChangesAsync();
            SetSweetAlertMessage("success", "Başarılı", "Ürün başarıyla oluşturuldu.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product model, IFormFile? Image, string? ImageUrl, bool DeleteImage = false)
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
                var (imagePath, imageFailed) = await ResolveImageOnEditAsync(Image, ImageUrl, model.Image, DeleteImage, _fileHelper, _uploadPath);
                if (imageFailed)
                {
                    SetSweetAlertMessage("error", "Hata", "Dosya yüklenirken hata oluştu. Lütfen geçerli bir görüntü dosyası seçin (JPG, PNG, GIF, WEBP).");
                    ViewBag.Categories = await _context.Categories.ToListAsync();
                    ViewBag.Brands = await _context.Brands.ToListAsync();
                    return View("Form", model);
                }
                model.Image = imagePath;

                _context.Products.Update(model);
                await _context.SaveChangesAsync();
                SetSweetAlertMessage("success", "Başarılı", "Ürün başarıyla güncellendi.");
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
                SetSweetAlertMessage("success", "Başarılı", "Ürün başarıyla silindi.");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                SetSweetAlertMessage("error", "Hata", $"Silme işlemi başarısız: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public Task<IActionResult> ImportExcel(IFormFile file)
            => ExcelImport(file, _excelHelper.ImportProductsAsync,
                (s, u) => $"{s} ürün eklendi, {u} ürün güncellendi.");

        [HttpGet]
        public IActionResult DownloadSampleExcel()
            => DownloadSampleExcel("urunler_ornek.xlsx",
                new[] {
                    ("urun_adi", true),
                    ("aciklama", false),
                    ("urun_kodu", false),
                    ("fiyat", false),
                    ("stok", false),
                    ("kategori", false),
                    ("marka", false),
                    ("durum", false),
                    ("anasayfa", false),
                    ("sira_numarasi", false)
                },
                new object[] { "Örnek Ürün", "Kaliteli bir ürün", "SKU-001", 99.90m, 50, "Örnek Kategori", "Örnek Marka Aş", "evet", "hayir", 1 });

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

            ws.Cells[1, 1].Value = "urun_adi";
            ws.Cells[1, 2].Value = "aciklama";
            ws.Cells[1, 3].Value = "urun_kodu";
            ws.Cells[1, 4].Value = "fiyat";
            ws.Cells[1, 5].Value = "stok";
            ws.Cells[1, 6].Value = "kategori";
            ws.Cells[1, 7].Value = "marka";
            ws.Cells[1, 8].Value = "durum";
            ws.Cells[1, 9].Value = "anasayfa";

            using (var headerRange = ws.Cells[1, 1, 1, 9])
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
                ws.Cells[row, 4].Value = p.Price;
                ws.Cells[row, 5].Value = p.StockCount;
                ws.Cells[row, 6].Value = p.Category?.Title;
                ws.Cells[row, 7].Value = p.Brand?.Name;
                ws.Cells[row, 8].Value = p.IsActive ? "evet" : "hayir";
                ws.Cells[row, 9].Value = p.IsHome ? "evet" : "hayir";
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
