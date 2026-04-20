using Core.Entities;
using Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Service.Extensions;
using System.Drawing;
using WebUI.Areas.Admin.Models;
using WebUI.Helper;

namespace WebUI.Areas.Admin.Controllers
{
    public class BrandsController : AdminBaseController
    {
        private readonly DatabaseContext _context;
        private readonly ExcelImportHelper _excelHelper;
        private readonly FileHelper _fileHelper;
        private readonly string _uploadPath;
        private readonly IWebHostEnvironment _env;

        public BrandsController(DatabaseContext context, ExcelImportHelper excelHelper, FileHelper fileHelper, IWebHostEnvironment env)
        {
            _context = context;
            _excelHelper = excelHelper;
            _fileHelper = fileHelper;
            _env = env;
            _uploadPath = Path.Combine(_env.WebRootPath, "uploads", "brands");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            List<Brand>? data = await _context.Brands
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Markalar"}
            };

            List<StartCardModel> startCards = new()
            {
                new StartCardModel
                {
                    Title = "Tüm Markalar",
                    Value = data.Count,
                    Class = "info",
                    Tooltip = "Sitede bulunan toplam marka sayısı",
                    Icon = "fa-solid fa-list"
                },
                new StartCardModel
                {
                    Title = "Aktif Markalar",
                    Value = data.Count(p => FunctionHelper.IsPublic(p.Status)),
                    Class = "success",
                    Tooltip = "Sitede aktif durumda olan marka sayısı",
                    Icon = "fa-solid fa-check"
                },
                new StartCardModel
                {
                    Title = "Taslak Markalar",
                    Value = data.Count(p =>  FunctionHelper.IsDraft(p.Status)),
                    Class = "secondary",
                    Tooltip = "Sitede taslak durumda olan marka sayısı",
                    Icon = "fa-solid fa-file"
                }
            };

            ViewBag.Breadcrumbs = breadcrumbs;
            ViewBag.StartCards = startCards;

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            Brand? data = await _context.Brands
                .FirstOrDefaultAsync(m => m.Id == id);

            if (data == null)
            {
                return NotFound();
            }

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Markalar", Controller= "Brands", Action = "Index" },
                new BreadcrumbItem { Title = data.Name }
            };

            ViewBag.Breadcrumbs = breadcrumbs;

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Form(int? id)
        {
            Brand? data = new();

            if (id.HasValue)
            {
                data = await _context.Brands.FindAsync(id);

                if (data == null)
                    return NotFound();
            }

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Markalar", Controller= "Brands", Action = "Index" },
                new BreadcrumbItem { Title = data?.Name ?? "Yeni Marka" }
            };

            ViewBag.Breadcrumbs = breadcrumbs;

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand model, IFormFile? Logo, string? LogoUrl)
        {
            if (!ModelState.IsValid)
            {
                return View("Form", model);
            }

            var (logoPath, logoFailed) = await ResolveImageAsync(Logo, LogoUrl, _fileHelper, _uploadPath);
            if (logoFailed) { SetSweetAlertMessage("Hata", "Dosya yüklenirken hata oluştu. Lütfen geçerli bir görüntü dosyası seçin (JPG, PNG, GIF, WEBP).", "error"); return View("Form", model); }
            if (logoPath != null) model.Logo = logoPath;

            model.Slug = FunctionHelper.GenerateSlug(model.Name);

            _context.Brands.Add(model);
            await _context.SaveChangesAsync();
            SetSweetAlertMessage("Başarılı", "Marka başarıyla oluşturuldu.", "success");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Brand model, IFormFile? Logo, string? LogoUrl, bool deleteLogo = false)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View("Form", model);
            }

            try
            {
                var (logoPath, logoFailed) = await ResolveImageOnEditAsync(Logo, LogoUrl, model.Logo, deleteLogo, _fileHelper, _uploadPath);
                if (logoFailed) { SetSweetAlertMessage("Hata", "Dosya yüklenirken hata oluştu. Lütfen geçerli bir görüntü dosyası seçin (JPG, PNG, GIF, WEBP).", "error"); return View("Form", model); }
                model.Logo = logoPath;

                model.Slug = FunctionHelper.GenerateSlug(model.Name);
                _context.Brands.Update(model);
                await _context.SaveChangesAsync();

                SetSweetAlertMessage("Başarılı", "Marka başarıyla güncellendi.", "success");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BrandExists(model.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var model = await _context.Brands.FindAsync(id);

            if (model == null)
            {
                return NotFound();
            }

            _context.Brands.Remove(model);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(model.Logo))
            {
                _fileHelper.Delete(model.Logo);
            }

            SetSweetAlertMessage("Başarılı", "Marka başarıyla silindi.", "success");
            return RedirectToAction(nameof(Index));
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

            var items = await _context.Brands.Where(b => ids.Contains(b.Id)).ToListAsync();
            foreach (var item in items)
                if (!string.IsNullOrEmpty(item.Logo))
                    _fileHelper.Delete(item.Logo);

            _context.Brands.RemoveRange(items);
            await _context.SaveChangesAsync();

            SetSweetAlertMessage("Başarılı", $"{items.Count} marka silindi.", "success");
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        public Task<IActionResult> ImportExcel(IFormFile file)
            => ExcelImport(file, _excelHelper.ImportBrandsAsync,
                (s, u) => $"{s} marka eklendi, {u} marka güncellendi.");

        [HttpGet]
        public IActionResult DownloadSampleExcel()
            => DownloadSampleExcel("markalar_ornek.xlsx",
                new[] {
                    (DisplayName<Brand>(nameof(Brand.Name)), true),
                    (DisplayName<Brand>(nameof(Brand.Description)), false),
                    (DisplayName<Brand>(nameof(Brand.Logo)), false),
                    (DisplayName<Brand>(nameof(Brand.Status)), false),
                    (DisplayName<Brand>(nameof(Brand.OrderNumber)), false)
                },
                new object[] { "Örnek Marka Aş", "Kaliteli ürünler sunan bir marka", "", 1, 1 });

        [HttpGet]
        public async Task<IActionResult> ExportExcel()
        {
            var brands = await _context.Brands
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Markalar");

            ws.Cells[1, 1].Value = DisplayName<Brand>(nameof(Brand.Name));
            ws.Cells[1, 2].Value = DisplayName<Brand>(nameof(Brand.Description));
            ws.Cells[1, 3].Value = DisplayName<Brand>(nameof(Brand.Logo));
            ws.Cells[1, 4].Value = DisplayName<Brand>(nameof(Brand.Status));
            ws.Cells[1, 5].Value = DisplayName<Brand>(nameof(Brand.OrderNumber));

            using (var headerRange = ws.Cells[1, 1, 1, 5])
            {
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0x4E, 0x73, 0xDF));
                headerRange.Style.Font.Color.SetColor(Color.White);
            }

            int row = 2;
            foreach (var b in brands)
            {
                ws.Cells[row, 1].Value = b.Name;
                ws.Cells[row, 2].Value = b.Description;
                ws.Cells[row, 3].Value = b.Logo;
                ws.Cells[row, 4].Value = b.Status;
                ws.Cells[row, 5].Value = b.OrderNumber;
                row++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            return File(package.GetAsByteArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"markalar_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }
        private bool BrandExists(int id)
        {
            return _context.Brands.Any(e => e.Id == id);
        }
    }
}
