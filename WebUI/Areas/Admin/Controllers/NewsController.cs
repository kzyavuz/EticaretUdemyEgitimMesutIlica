using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Data.Context;
using WebUI.Helper;

namespace WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
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
            return View(await _context.News.ToListAsync());
        }

        [HttpPost]
        public Task<IActionResult> ImportExcel(IFormFile file)
            => ExcelImport(file, _excelHelper.ImportNewsAsync,
                (s, u) => $"{s} haber eklendi, {u} haber güncellendi.");

        [HttpGet]
        public IActionResult DownloadSampleExcel()
            => DownloadSampleExcel("haberler_ornek.xlsx",
                new[] {
                    ("title", true),
                    ("description", false),
                    ("image", false),
                    ("isactive", false),
                    ("sira_numarasi", false)
                },
                new object[] { "Örnek Haber Başlığı", "Haber içeriği buraya gelecek", "", "evet", 1 });

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var model = await _context.News
                .FirstOrDefaultAsync(m => m.Id == id);
            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        public async Task<IActionResult> Form(int? id)
        {
            if (id == null)
            {
                return View();
            }

            var model = await _context.News.FindAsync(id);

            if (model == null)
            {
                return NotFound();
            }
            return View(model);
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
            SetSweetAlertMessage("Başarılı", "Haber başarıyla oluşturuldu.", "success");

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
                    SetSweetAlertMessage("Başarılı", "Haber başarıyla güncellendi.", "success");
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
                SetSweetAlertMessage("Hata", "Haber bulunamadı.", "error");
                return RedirectToAction(nameof(Index));
            }

            _context.News.Remove(model);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(model.Image))
                _fileHelper.Delete(model.Image);

            SetSweetAlertMessage("Başarılı", "Haber başarıyla silindi.", "success");
            return RedirectToAction(nameof(Index));
        }

        private bool NewsExists(int id)
        {
            return _context.News.Any(e => e.Id == id);
        }
    }
}
