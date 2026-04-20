using Core.Entities;
using Service.Extensions;
using Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUI.Areas.Admin.Models;
using WebUI.Helper;

namespace WebUI.Areas.Admin.Controllers
{
    public class NewsController : AdminBaseController
    {
        private readonly DatabaseContext _context;
        private readonly ExcelImportHelper _excelHelper;
        private readonly FileHelper _fileHelper;
        private readonly string _uploadPath;
        private readonly IWebHostEnvironment _env;

        public NewsController(DatabaseContext context, ExcelImportHelper excelHelper, FileHelper fileHelper, IWebHostEnvironment env)
        {
            _context = context;
            _excelHelper = excelHelper;
            _fileHelper = fileHelper;
            _env = env;
            _uploadPath = Path.Combine(_env.WebRootPath, "uploads", "categories");
        }

        public async Task<IActionResult> Index()
        {
            List<News>? data = await _context.News
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Kampanyalar"}
            };

            List<StartCardModel> startCards = new()
            {
                new StartCardModel
                {
                    Title = "Tüm Kampanyalar",
                    Value = data.Count,
                    Class = "info",
                    Tooltip = "Sitede bulunan toplam duyuru sayısı",
                    Icon = "fa-solid fa-list"
                },
                new StartCardModel
                {
                    Title = "Aktif Kampanyalar",
                    Value = data.Count(p => FunctionHelper.IsActive(p.Status)),
                    Class = "success",
                    Tooltip = "Sitede aktif durumda olan duyuru sayısı",
                    Icon = "fa-solid fa-check"
                },
                new StartCardModel
                {
                    Title = "Taslak Kampanyalar",
                    Value = data.Count(p => FunctionHelper.IsDraft(p.Status)),
                    Class = "secondary",
                    Tooltip = "Sitede taslak durumda olan duyuru sayısı",
                    Icon = "fa-solid fa-file"
                }
            };

            ViewBag.Breadcrumbs = breadcrumbs;
            ViewBag.StartCards = startCards;

            return View(data);
        }

        public async Task<IActionResult> Details(int id)
        {
            News? data = await _context.News
                .FirstOrDefaultAsync(m => m.Id == id);

            if (data == null)
            {
                return NotFound();
            }

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Kampanyalar", Controller= "News", Action = "Index" },
                new BreadcrumbItem { Title = data.Title }
            };

            ViewBag.Breadcrumbs = breadcrumbs;

            return View(data);
        }

        public async Task<IActionResult> Form(int? id)
        {
            News? data = new();

            if (id.HasValue)
            {
                data = await _context.News.FindAsync(id);

                if (data == null)
                    return NotFound();
            }

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Kampanyalar", Controller= "News", Action = "Index" },
                new BreadcrumbItem { Title = data?.Title ?? "Yeni Kampanya" }
            };

            ViewBag.Breadcrumbs = breadcrumbs;

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(News model, IFormFile? Image, string? ImageUrl)
        {
            if (!ModelState.IsValid)
                return View("Form", model);

            var (imagePath, imageFailed) = await ResolveImageAsync(Image, ImageUrl, _fileHelper, _uploadPath);
            if (imageFailed) { SetSweetAlertMessage("Hata", "Dosya yüklenirken hata oluştu. Lütfen geçerli bir görüntü dosyası seçin (JPG, PNG, GIF, WEBP).", "error"); return View("Form", model); }
            if (imagePath != null) model.Image = imagePath;

            _context.Add(model);
            await _context.SaveChangesAsync();
            SetSweetAlertMessage("Başarılı", "Kampanya başarıyla oluşturuldu.", "success");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, News model, IFormFile? Image, string? ImageUrl, bool deleteImage = false)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var (imagePath, imageFailed) = await ResolveImageOnEditAsync(Image, ImageUrl, model.Image, deleteImage, _fileHelper, _uploadPath);
                    if (imageFailed) { SetSweetAlertMessage("Hata", "Dosya yüklenirken hata oluştu. Lütfen geçerli bir görüntü dosyası seçin (JPG, PNG, GIF, WEBP).", "error"); return View("Form", model); }
                    model.Image = imagePath;

                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    SetSweetAlertMessage("Başarılı", "Kampanya başarıyla güncellendi.", "success");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NewsExists(model.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View("Form", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var model = await _context.News.FindAsync(id);

            if (model == null)
            {
                SetSweetAlertMessage("Hata", "Kampanya bulunamadı.", "error");
                return RedirectToAction(nameof(Index));
            }

            _context.News.Remove(model);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(model.Image))
                _fileHelper.Delete(model.Image);

            SetSweetAlertMessage("Başarılı", "Kampanya başarıyla silindi.", "success");
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

            var items = await _context.News.Where(n => ids.Contains(n.Id)).ToListAsync();
            foreach (var item in items)
                if (!string.IsNullOrEmpty(item.Image))
                    _fileHelper.Delete(item.Image);

            _context.News.RemoveRange(items);
            await _context.SaveChangesAsync();

            SetSweetAlertMessage("Başarılı", $"{items.Count} haber silindi.", "success");
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        public Task<IActionResult> ImportExcel(IFormFile file)
            => ExcelImport(file, _excelHelper.ImportNewsAsync,
                (s, u) => $"{s} haber eklendi, {u} haber güncellendi.");

        [HttpGet]
        public IActionResult DownloadSampleExcel()
            => DownloadSampleExcel("haberler_ornek.xlsx",
                new[] {
                    (DisplayName<News>(nameof(News.Title)), true),
                    (DisplayName<News>(nameof(News.Description)), false),
                    (DisplayName<News>(nameof(News.Image)), false),
                    (DisplayName<News>(nameof(News.Status)), false),
                    (DisplayName<News>(nameof(News.OrderNumber)), false)
                },
                new object[] { "Örnek Kampanya Başlığı", "Kampanya içeriği buraya gelecek", "", "evet", 1 });

        private bool NewsExists(int id)
        {
            return _context.News.Any(e => e.Id == id);
        }
    }
}
