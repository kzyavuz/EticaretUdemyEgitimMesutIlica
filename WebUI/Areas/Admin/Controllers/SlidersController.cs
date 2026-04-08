using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Data.Context;
using WebUI.Helper;

namespace WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
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
            return View(await _context.Sliders.ToListAsync());
        }

        [HttpPost]
        public Task<IActionResult> ImportExcel(IFormFile file)
            => ExcelImport(file, _excelHelper.ImportSlidersAsync,
                (s, u) => $"{s} slider eklendi, {u} slider güncellendi.");

        [HttpGet]
        public IActionResult DownloadSampleExcel()
            => DownloadSampleExcel("sliderlar_ornek.xlsx",
                new[] {
                    ("title", true),
                    ("description", false),
                    ("image", false),
                    ("link", false),
                    ("isactive", false),
                    ("sira_numarasi", false)
                },
                new object[] { "Örnek Slider Başlığı", "Slider alt yazısı", "", "/urunler", "evet", 1 });

        // GET: Admin/Sliders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var slider = await _context.Sliders.FirstOrDefaultAsync(m => m.Id == id);
            if (slider == null) return NotFound();

            return View(slider);
        }

        public async Task<IActionResult> Form(int? id)
        {
            if (id == null) return View();

            var slider = await _context.Sliders.FindAsync(id);
            if (slider == null) return NotFound();

            return View(slider);
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

        private bool SliderExists(int id)
        {
            return _context.Sliders.Any(e => e.Id == id);
        }
    }
}
