using Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebUI.Areas.Admin.Models;

namespace WebUI.Areas.Admin.Controllers
{
    public class AppRolesController : AdminBaseController
    {
        private readonly RoleManager<AppRole> _roleManager;

        public AppRolesController(RoleManager<AppRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            var roles = _roleManager.Roles.ToList();

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Roller" }
            };
            ViewBag.Breadcrumbs = breadcrumbs;

            return View(roles);
        }

        public async Task<IActionResult> Details(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null) return NotFound();

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Roller", Controller = "AppRoles", Action = "Index" },
                new BreadcrumbItem { Title = role.Name }
            };

            ViewBag.Breadcrumbs = breadcrumbs;

            return View(role);
        }

        public async Task<IActionResult> Form(int? id)
        {
            AppRole? data = new();

            if (id.HasValue)
            {
                data = await _roleManager.FindByIdAsync(id.Value.ToString());
                if (data == null) return NotFound();
            }

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Roller", Controller = "AppRoles", Action = "Index" },
                new BreadcrumbItem { Title = data.Name ?? "Yeni Rol" }
            };

            ViewBag.Breadcrumbs = breadcrumbs;
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppRole model)
        {
            if (!ModelState.IsValid) return View("Form", model);

            model.NormalizedName = model.Name!.ToUpperInvariant();
            var result = await _roleManager.CreateAsync(model);

            if (result.Succeeded)
            {
                SetSweetAlertMessage("Başarılı", $"\"{model.Name}\" rolü eklendi.", "success");
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View("Form", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AppRole model)
        {
            if (!ModelState.IsValid) return View("Form", model);

            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null) return NotFound();

            role.Name = model.Name;
            role.NormalizedName = model.Name!.ToUpperInvariant();

            var result = await _roleManager.UpdateAsync(role);

            if (result.Succeeded)
            {
                SetSweetAlertMessage("Başarılı", $"\"{role.Name}\" rolü güncellendi.", "success");
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View("Form", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                SetSweetAlertMessage("Hata", "Rol bulunamadı.", "error");
                return RedirectToAction(nameof(Index));
            }

            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
                SetSweetAlertMessage("Başarılı", $"\"{role.Name}\" rolü silindi.", "success");
            else
                SetSweetAlertMessage("Hata", string.Join(", ", result.Errors.Select(e => e.Description)), "error");

            return RedirectToAction(nameof(Index));
        }
    }
}
