using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Data.Context;
using WebUI.Helper;

namespace WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
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

        public async Task<IActionResult> Index()
        {
            return View(await _context.Brands.ToListAsync());
        }

        [HttpPost]
        public Task<IActionResult> ImportExcel(IFormFile file)
            => ExcelImport(file, _excelHelper.ImportBrandsAsync,
                (s, u) => $"{s} marka eklendi, {u} marka güncellendi.");

        [HttpGet]
        public IActionResult DownloadSampleExcel()
            => DownloadSampleExcel("markalar_ornek.xlsx",
                new[] {
                    ("marka_adi", true),
                    ("aciklama", false),
                    ("logo", false),
                    ("durum", false),
                    ("sira_numarasi", false)
                },
                new object[] { "Örnek Marka Aş", "Kaliteli ürünler sunan bir marka", "", "evet", 1 });

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var model = await _context.Brands
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

            var model = await _context.Brands.FindAsync(id);

            if (model == null)
            {
                return NotFound();
            }
            return View(model);
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

            if (ModelState.IsValid)
            {
                try
                {
                    var (logoPath, logoFailed) = await ResolveImageOnEditAsync(Logo, LogoUrl, model.Logo, deleteLogo, _fileHelper, _uploadPath);
                    if (logoFailed) { SetSweetAlertMessage("Hata", "Dosya yüklenirken hata oluştu. Lütfen geçerli bir görüntü dosyası seçin (JPG, PNG, GIF, WEBP).", "error"); return View("Form", model); }
                    model.Logo = logoPath;

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
            return View("Form", model);
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

        private bool BrandExists(int id)
        {
            return _context.Brands.Any(e => e.Id == id);
        }
    }
}
