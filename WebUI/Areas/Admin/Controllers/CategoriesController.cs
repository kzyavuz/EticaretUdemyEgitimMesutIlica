using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Data.Context;
using WebUI.Helper;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoriesController : AdminBaseController
    {
        private readonly DatabaseContext _context;
        private readonly ExcelImportHelper _excelHelper;
        private readonly FileHelper _fileHelper;
        private readonly string _uploadPath;
        private readonly IWebHostEnvironment _env;

        public CategoriesController(DatabaseContext context, ExcelImportHelper excelHelper, FileHelper fileHelper, IWebHostEnvironment env)
        {
            _context = context;
            _excelHelper = excelHelper;
            _fileHelper = fileHelper;
            _env = env;
            _uploadPath = Path.Combine(_env.WebRootPath, "uploads", "categories");
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Categories.ToListAsync());
        }

        [HttpPost]
        public Task<IActionResult> ImportExcel(IFormFile file)
            => ExcelImport(file, _excelHelper.ImportCategoriesAsync,
                (s, u) => $"{s} kategori eklendi, {u} kategori güncellendi.");

        [HttpGet]
        public IActionResult DownloadSampleExcel()
            => DownloadSampleExcel("kategoriler_ornek.xlsx",
                new[] {
                    ("title", true),
                    ("description", false),
                    ("image", false),
                    ("istop menu", false),
                    ("isactive", false),
                    ("sira_numarasi", false)
                },
                new object[] { "Örnek Kategori", "Kategori açıklaması", "", "evet", "evet", 1 });

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var model = await _context.Categories
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
                ViewBag.Categories = await GetCategoriesAsync();
                return View();
            }

            var model = await _context.Categories.FindAsync(id);

            if (model == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await GetCategoriesAsync();
            return View("Form", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category model, IFormFile? Image, string? ImageUrl)
        {
            if (!ModelState.IsValid)
            {
                return View("Form", model);
            }

            _context.Add(model);
            await _context.SaveChangesAsync();

            var (imagePath, imageFailed) = await ResolveImageAsync(Image, ImageUrl, _fileHelper, _uploadPath);
            if (imageFailed) { SetSweetAlertMessage("Hata", "Dosya yüklenirken hata oluştu. Lütfen geçerli bir görüntü dosyası seçin (JPG, PNG, GIF, WEBP).", "error"); return View("Form", model); }
            if (imagePath != null) model.Image = imagePath;

            SetSweetAlertMessage("Başarılı", "Kategori başarıyla oluşturuldu.", "success");
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category model, IFormFile? Image, string? ImageUrl, bool deleteImage = false)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var (imagePath, imageFailed) = await ResolveImageOnEditAsync(Image, ImageUrl, model.Image, deleteImage, _fileHelper, _uploadPath);
                    if (imageFailed) { SetSweetAlertMessage("Hata", "Dosya yüklenirken hata oluştu. Lütfen geçerli bir görüntü dosyası seçin (JPG, PNG, GIF, WEBP).", "error"); return View("Form", model); }
                    model.Image = imagePath;

                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    SetSweetAlertMessage("Başarılı", "Kategori başarıyla güncellendi.", "success");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(model.Id))
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
            ViewBag.Categories = await GetCategoriesAsync();
            return View("Form", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var model = await _context.Categories.FindAsync(id);

            if (model == null)
            {
                return NotFound();
            }

            _context.Categories.Remove(model);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(model.Image))
            {
                _fileHelper.Delete(model.Image);
            }

            SetSweetAlertMessage("Başarılı", "Kategori başarıyla silindi.", "success");

            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }

        private async Task<SelectList> GetCategoriesAsync()
        {
            var categories = await _context.Categories.ToListAsync();
            return new SelectList(categories, "Id", "Title");
        }
    }
}
