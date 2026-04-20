using Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUI.Areas.Admin.Models;
using WebUI.Helper;

namespace WebUI.Areas.Admin.Controllers
{
    public class AppUsersController : AdminBaseController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly FileHelper _fileHelper;
        private readonly string _uploadPath;
        private readonly IWebHostEnvironment _env;

        public AppUsersController(
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            FileHelper fileHelper,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _fileHelper = fileHelper;
            _env = env;
            _uploadPath = Path.Combine(_env.WebRootPath, "uploads", "users");
        }

        public async Task<IActionResult> Index()
        {
            // _context yerine _userManager.Users kullanıyoruz
            List<AppUser> users = await _userManager.Users.ToListAsync();

            var userRoles = new Dictionary<int, List<string>>();
            foreach (var user in users)
            {
                userRoles[user.Id] = (await _userManager.GetRolesAsync(user)).ToList();
            }

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Kullanıcılar" }
            };

            List<StartCardModel> startCards = new()
            {
                new StartCardModel
                {
                    Title = "Tüm Kullanıcılar",
                    Value = users.Count,
                    Class = "info",
                    Tooltip = "Sitede bulunan toplam admin sayısı",
                    Icon = "fa-solid fa-user"
                },
                new StartCardModel
                {
                    Title = "Aktif Kullanıcılar",
                    Value = users.Count(p => p.IsActive),
                    Class = "success",
                    Tooltip = "Sitede aktif durumda olan admin sayısı",
                    Icon = "fa-solid fa-check"
                },
                new StartCardModel
                {
                    Title = "Taslak Kullanıcılar",
                    Value = users.Count(p => !p.IsActive),
                    Class = "secondary",
                    Tooltip = "Sitede pasif durumda olan admin sayısı",
                    Icon = "fa-solid fa-file"
                }
            };

            ViewBag.Breadcrumbs = breadcrumbs;
            ViewBag.StartCards = startCards;
            ViewBag.UserRoles = userRoles;

            return View(users);
        }

        public async Task<IActionResult> Details(int id)
        {
            AppUser? data = await _userManager.FindByIdAsync(id.ToString());

            if (data == null)
                return NotFound();

            var userRoles = await _userManager.GetRolesAsync(data);

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Kullanıcılar", Controller = "AppUsers", Action = "Index" },
                new BreadcrumbItem { Title = data.FullName }
            };

            ViewBag.Breadcrumbs = breadcrumbs;
            ViewBag.UserRoles = userRoles;

            return View(data);
        }

        public async Task<IActionResult> Form(int? id)
        {
            AppUser data = new();

            if (id.HasValue)
            {
                var found = await _userManager.FindByIdAsync(id.Value.ToString());

                if (found == null)
                    return NotFound();

                data = found;
            }

            var userRoles = await _userManager.GetRolesAsync(data);

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Kullanıcılar", Controller = "AppUsers", Action = "Index" },
                new BreadcrumbItem { Title = data?.FullName ?? "Yeni Kullanıcı" }
            };

            ViewBag.Breadcrumbs = breadcrumbs;

            ViewBag.Roles = await GetAllRoles();
            ViewBag.UserRoles = userRoles;

            return View(data);
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
                ModelState.AddModelError("password", "Şifre gereklidir.");
                ViewBag.Roles = await GetAllRoles();
                return View("Form", model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email ?? "");
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Bu e-posta adresi zaten kayıtlı.");
                ViewBag.Roles = await GetAllRoles();
                return View("Form", model);
            }

            var (imagePath, imageFailed) = await ResolveImageAsync(Image, ImageUrl, _fileHelper, _uploadPath);
            if (imageFailed)
            {
                SetSweetAlertMessage("Hata", "Dosya yüklenirken hata oluştu. Lütfen geçerli bir görüntü dosyası seçin (JPG, PNG, GIF, WEBP).", "error");
                return View("Form", model);
            }

            if (imagePath != null)
                model.Image = imagePath;

            var user = new AppUser
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                UserName = model.UserName,
                PhoneNumber = model.PhoneNumber,
                Image = model.Image,
                IsActive = model.IsActive
                // SecurityStamp ve ConcurrencyStamp → CreateAsync otomatik set eder
            };

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                ViewBag.Roles = await GetAllRoles();
                return View("Form", model);
            }

            // Rol atama
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

            SetSweetAlertMessage("Başarılı", "Kullanıcı başarıyla oluşturuldu.", "success");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AppUser model, int[] roleIds, IFormFile? Image, string? ImageUrl, bool deleteImage = false)
        {
            if (id != model.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                var userRolesInvalid = await _userManager.GetRolesAsync(model);
                ViewBag.UserRoles = userRolesInvalid;
                ViewBag.Roles = await GetAllRoles();
                return View("Form", model);
            }

            try
            {
                var existingUser = await _userManager.FindByIdAsync(id.ToString());

                if (existingUser == null)
                    return NotFound();

                var (imagePath, imageFailed) = await ResolveImageOnEditAsync(Image, ImageUrl, existingUser.Image, deleteImage, _fileHelper, _uploadPath);
                if (imageFailed)
                {
                    SetSweetAlertMessage("Hata", "Dosya yüklenirken hata oluştu. Lütfen geçerli bir görüntü dosyası seçin (JPG, PNG, GIF, WEBP).", "error");
                    ViewBag.Roles = await GetAllRoles();
                    ViewBag.UserRoles = await _userManager.GetRolesAsync(existingUser);
                    return View("Form", model);
                }

                // Email ve UserName için Identity'nin kendi metodlarını kullan
                // Böylece NormalizedEmail / NormalizedUserName otomatik güncellenir
                if (existingUser.Email != model.Email)
                {
                    var setEmailResult = await _userManager.SetEmailAsync(existingUser, model.Email);
                    if (!setEmailResult.Succeeded)
                    {
                        foreach (var error in setEmailResult.Errors)
                            ModelState.AddModelError("", error.Description);

                        ViewBag.Roles = await GetAllRoles();
                        ViewBag.UserRoles = await _userManager.GetRolesAsync(existingUser);
                        return View("Form", model);
                    }
                }

                if (existingUser.UserName != model.UserName)
                {
                    var setUserNameResult = await _userManager.SetUserNameAsync(existingUser, model.UserName);
                    if (!setUserNameResult.Succeeded)
                    {
                        foreach (var error in setUserNameResult.Errors)
                            ModelState.AddModelError("", error.Description);

                        ViewBag.Roles = await GetAllRoles();
                        ViewBag.UserRoles = await _userManager.GetRolesAsync(existingUser);
                        return View("Form", model);
                    }
                }

                // Diğer alanları güncelle
                existingUser.FirstName = model.FirstName;
                existingUser.LastName = model.LastName;
                existingUser.PhoneNumber = model.PhoneNumber;
                existingUser.Image = imagePath;
                existingUser.IsActive = model.IsActive;

                var result = await _userManager.UpdateAsync(existingUser);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error.Description);

                    ViewBag.Roles = await GetAllRoles();
                    ViewBag.UserRoles = await _userManager.GetRolesAsync(existingUser);
                    return View("Form", model);
                }

                // Rol güncelleme
                var currentRoles = await _userManager.GetRolesAsync(existingUser);

                var selectedRoles = new List<string>();
                if (roleIds != null && roleIds.Length > 0)
                {
                    selectedRoles = await _roleManager.Roles
                        .Where(r => roleIds.Contains(r.Id))
                        .Select(r => r.Name ?? "")
                        .ToListAsync();
                }

                var rolesToRemove = currentRoles.Except(selectedRoles).ToList();
                var rolesToAdd = selectedRoles.Except(currentRoles).ToList();

                if (rolesToRemove.Any())
                    await _userManager.RemoveFromRolesAsync(existingUser, rolesToRemove);

                if (rolesToAdd.Any())
                    await _userManager.AddToRolesAsync(existingUser, rolesToAdd);

                SetSweetAlertMessage("Başarılı", "Kullanıcı başarıyla güncellendi.", "success");
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await AppUserExists(id))
                    return NotFound();

                throw;
            }
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

        // ─── Private Helpers ────────────────────────────────────────────────────

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