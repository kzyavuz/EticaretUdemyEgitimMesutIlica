using Core.Entities;
using Service.Extensions;
using Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUI.Areas.Admin.Models;
using WebUI.Helper;

namespace WebUI.Areas.Admin.Controllers
{
    public class SlidersController : AdminBaseController
    {
        private readonly DatabaseContext _context;
        private readonly ExcelImportHelper _excelHelper;
        private readonly FileHelper _fileHelper;
        private readonly string _uploadPath;

        public SlidersController(DatabaseContext context, ExcelImportHelper excelHelper, FileHelper fileHelper, IWebHostEnvironment env)
        {
            _context = context;
            _excelHelper = excelHelper;
            _fileHelper = fileHelper;
            _uploadPath = Path.Combine(env.WebRootPath, "uploads", "sliders");
        }

        // GET: Admin/Sliders
        public async Task<IActionResult> Index()
        {
            List<Slider>? data = await _context.Sliders
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Sliderlar"}
            };

            List<StartCardModel> startCards = new()
            {
                new StartCardModel
                {
                    Title = "Tüm Sliderlar",
                    Value = data.Count,
                    Class = "info",
                    Tooltip = "Sitede bulunan toplam slider sayısı",
                    Icon = "fa-solid fa-list"
                },
                new StartCardModel
                {
                    Title = "Aktif Sliderlar",
                    Value = data.Count(p => FunctionHelper.IsActive(p.Status)),
                    Class = "success",
                    Tooltip = "Sitede aktif durumda olan slider sayısı",
                    Icon = "fa-solid fa-check"
                },
                new StartCardModel
                {
                    Title = "Taslak Sliderlar",
                    Value = data.Count(p => FunctionHelper.IsDraft(p.Status)),
                    Class = "secondary",
                    Tooltip = "Sitede taslak durumda olan slider sayısı",
                    Icon = "fa-solid fa-file"
                }
            };

            ViewBag.Breadcrumbs = breadcrumbs;
            ViewBag.StartCards = startCards;

            return View(data);
        }

        // GET: Admin/Sliders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            Slider? data = await _context.Sliders
                .FirstOrDefaultAsync(m => m.Id == id);

            if (data == null)
            {
                return NotFound();
            }

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Sliderlar", Controller= "Sliders", Action = "Index" },
                new BreadcrumbItem { Title = data.Title }
            };

            ViewBag.Breadcrumbs = breadcrumbs;

            return View(data);
        }

        public async Task<IActionResult> Form(int? id)
        {
            Slider? data = new();

            if (id.HasValue)
            {
                data = await _context.Sliders.FindAsync(id);
                if (data == null)
                    return NotFound();
            }

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Sliderlar", Controller= "Sliders", Action = "Index" },
                new BreadcrumbItem { Title = data?.Title ?? "Yeni Slider" }
            };

            ViewBag.Breadcrumbs = breadcrumbs;

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Slider model, IFormFile? Image, string? ImageUrl)
        {
            if (!ModelState.IsValid)
                return View("Form", model);

            var (imagePath, imageFailed) = await ResolveImageAsync(Image, ImageUrl, _fileHelper, _uploadPath);
            if (imageFailed) { SetSweetAlertMessage("Hata", "Dosya yüklenirken hata oluştu. Lütfen geçerli bir görüntü dosyası seçin (JPG, PNG, GIF, WEBP).", "error"); return View("Form", model); }
            if (imagePath != null) model.Image = imagePath;

            _context.Add(model);
            await _context.SaveChangesAsync();
            SetSweetAlertMessage("Başarılı", "Slider başarıyla oluşturuldu.", "success");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Slider model, IFormFile? Image, string? ImageUrl, bool deleteImage = false)
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
                    SetSweetAlertMessage("Başarılı", "Slider başarıyla güncellendi.", "success");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SliderExists(model.Id)) return NotFound();
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
            var slider = await _context.Sliders.FindAsync(id);

            if (slider == null)
            {
                SetSweetAlertMessage("Hata", "Slider bulunamadı.", "error");
                return RedirectToAction(nameof(Index));
            }

            _context.Sliders.Remove(slider);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(slider.Image))
                _fileHelper.Delete(slider.Image);

            SetSweetAlertMessage("Başarılı", "Slider başarıyla silindi.", "success");
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

            var items = await _context.Sliders.Where(s => ids.Contains(s.Id)).ToListAsync();
            foreach (var item in items)
                if (!string.IsNullOrEmpty(item.Image))
                    _fileHelper.Delete(item.Image);

            _context.Sliders.RemoveRange(items);
            await _context.SaveChangesAsync();

            SetSweetAlertMessage("Başarılı", $"{items.Count} slider silindi.", "success");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public Task<IActionResult> ImportExcel(IFormFile file)
            => ExcelImport(file, _excelHelper.ImportSlidersAsync,
                (s, u) => $"{s} slider eklendi, {u} slider güncellendi.");

        [HttpGet]
        public IActionResult DownloadSampleExcel()
            => DownloadSampleExcel("sliderlar_ornek.xlsx",
                new[] {
                    (DisplayName<Slider>(nameof(Slider.Title)), true),
                    (DisplayName<Slider>(nameof(Slider.Description)), false),
                    (DisplayName<Slider>(nameof(Slider.Image)), false),
                    ("Bağlantı", false),
                    (DisplayName<Slider>(nameof(Slider.Status)), false),
                    (DisplayName<Slider>(nameof(Slider.OrderNumber)), false)
                },
                new object[] { "Örnek Slider Başlığı", "Slider alt yazısı", "", "/urunler", "Aktif", 1 });
        private bool SliderExists(int id)
        {
            return _context.Sliders.Any(e => e.Id == id);
        }
    }
}
