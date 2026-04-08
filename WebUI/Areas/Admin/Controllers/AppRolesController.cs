using Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
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
            return View(roles);
        }

        public async Task<IActionResult> Details(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null) return NotFound();
            return View(role);
        }

        public async Task<IActionResult> Form(int? id)
        {
            if (id == null) return View(new AppRole());

            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null) return NotFound();
            return View(role);
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
