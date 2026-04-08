using Core.Entities;
using Data.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUI.Helper;

namespace WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AppUsersController : AdminBaseController
    {
        private readonly DatabaseContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly FileHelper _fileHelper;
        private readonly string _uploadPath;
        private readonly IWebHostEnvironment _env;

        public AppUsersController(DatabaseContext context, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, FileHelper fileHelper, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _fileHelper = fileHelper;
            _env = env;
            _uploadPath = Path.Combine(_env.WebRootPath, "uploads", "users");
        }

        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();

            // Her kullanıcının rollerini al
            var userRoles = new Dictionary<int, List<string>>();
            foreach (var user in users)
            {
                userRoles[user.Id] = (await _userManager.GetRolesAsync(user)).ToList();
            }

            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var model = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (model == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(model);
            ViewBag.UserRoles = userRoles;

            return View(model);
        }


        public async Task<IActionResult> Form(int? id)
        {
            if (id == null)
            {
                ViewBag.Roles = await GetAllRoles();
                return View();
            }

            var model = await _context.Users.FindAsync(id);

            if (model == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(model);

            ViewBag.Roles = await GetAllRoles();
            ViewBag.UserRoles = userRoles;

            return View("Form", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppUser model, string password, int[] roleIds, IFormFile? Image, string? ImageUrl)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await GetAllRoles();
                return View("Form", model);
            }

            if (string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("password", "Şifre gereklidir");
                ViewBag.Roles = await GetAllRoles();
                return View("Form", model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email ?? "");

            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Bu e-posta adresi zaten kayıtlı.");
                ViewBag.Roles = await GetAllRoles(); ;
                return View("Form", model);
            }

            var (imagePath, imageFailed) = await ResolveImageAsync(Image, ImageUrl, _fileHelper, _uploadPath);
            if (imageFailed) { SetSweetAlertMessage("Hata", "Dosya yüklenirken hata oluştu. Lütfen geçerli bir görüntü dosyası seçin (JPG, PNG, GIF, WEBP).", "error"); return View("Form", model); }
            if (imagePath != null) model.Image = imagePath;

            // Yeni user oluştur dengan stamps
            var user = new AppUser
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                UserName = model.UserName,
                PhoneNumber = model.PhoneNumber,
                Image = model.Image,
                IsActive = model.IsActive,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

            // UserManager ile user oluştur
            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                SetSweetAlertMessage("Başarılı", "Kullanıcı başarıyla oluşturuldu.", "success");
                // Seçilen roller ekle
                if (roleIds != null && roleIds.Length > 0)
                {
                    var selectedRoles = await _roleManager.Roles
                        .Where(r => roleIds.Contains(r.Id))
                        .Select(r => r.Name ?? "")
                        .ToListAsync();

                    var roleResult = await _userManager.AddToRolesAsync(user, selectedRoles);

                    if (!roleResult.Succeeded)
                    {
                        foreach (var error in roleResult.Errors)
                            ModelState.AddModelError("", $"Rol atama hatası: {error.Description}");

                        await _userManager.DeleteAsync(user);

                        ViewBag.Roles = await GetAllRoles();
                        return View("Form", model);
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            else
            {
                // Hataları ModelState'e ekle
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            ViewBag.Roles = await GetAllRoles();
            return View("Form", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AppUser model, int[] roleIds, IFormFile? Image, string? ImageUrl, bool deleteImage = false)
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

                    // UserManager ile user'ı al
                    var existingUser = await _userManager.FindByIdAsync(id.ToString());

                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    // Sadece düzenlenebilir alanları güncelle
                    existingUser.FirstName = model.FirstName;
                    existingUser.LastName = model.LastName;
                    existingUser.Email = model.Email;
                    existingUser.PhoneNumber = model.PhoneNumber;
                    existingUser.Image = model.Image;
                    existingUser.IsActive = model.IsActive;

                    // SecurityStamp varsa koru, yoksa initialize et
                    if (string.IsNullOrEmpty(existingUser.SecurityStamp))
                    {
                        existingUser.SecurityStamp = Guid.NewGuid().ToString();
                    }

                    // UserManager.UpdateAsync kullan
                    var result = await _userManager.UpdateAsync(existingUser);

                    if (result.Succeeded)
                    {
                        // Mevcut roller al
                        var currentRoles = await _userManager.GetRolesAsync(existingUser);

                        // Seçilen roller
                        var selectedRoles = new List<string>();
                        if (roleIds != null && roleIds.Length > 0)
                        {
                            selectedRoles = await _roleManager.Roles
                                .Where(r => roleIds.Contains(r.Id))
                                .Select(r => r.Name ?? "")
                                .ToListAsync();
                        }

                        // Çıkarılacak roller (current'ten selected'a olmayan)
                        var rolesToRemove = currentRoles.Except(selectedRoles).ToList();

                        // Eklenecek roller (selected'dan current'e olmayan)
                        var rolesToAdd = selectedRoles.Except(currentRoles).ToList();

                        // Roller kaldır
                        if (rolesToRemove.Any())
                        {
                            await _userManager.RemoveFromRolesAsync(existingUser, rolesToRemove);
                        }

                        // Roller ekle
                        if (rolesToAdd.Any())
                        {
                            await _userManager.AddToRolesAsync(existingUser, rolesToAdd);
                        }

                        SetSweetAlertMessage("Başarılı", "Kullanıcı başarıyla güncellendi.", "success");
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        // Hataları ModelState'e ekle
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await AppUserExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            var userRoles = await _userManager.GetRolesAsync(model);

            ViewBag.UserRoles = userRoles;
            ViewBag.Roles = await GetAllRoles();

            return View("Form", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var model = await _userManager.FindByIdAsync(id.ToString());

            if (model == null)
            {
                SetSweetAlertMessage("Hata", "Kullanıcı bulunamadı.", "error");
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(model);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(model.Image))
                    _fileHelper.Delete(model.Image);

                SetSweetAlertMessage("Başarılı", "Kullanıcı başarıyla silindi.", "success");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                SetSweetAlertMessage("Hata", $"Silme işlemi başarısız: {errors}", "error");
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> AppUserExists(int id)
        {
            return await _userManager.FindByIdAsync(id.ToString()) != null;
        }

        private async Task<List<AppRole>> GetAllRoles()
        {
            return await _roleManager.Roles.ToListAsync();
            
        }
    }
}
