using Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebUI.Areas.Customer.Controllers;
using WebUI.Areas.Customer.Models;

public class AccountController : CustomerBaseController
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [Route("profilim")]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var viewModel = new ProfileViewModel
        {
            EditProfileViewModel = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.PhoneNumber,
                UserName = user.UserName,
                CreatedDate = user.CreatedDate,
                ProfilImage = user.Image,
                IsTwoFactor = user.TwoFactorEnabled
            },
            ChangePasswordViewModel = new ChangePasswordViewModel()
        };

        return View(viewModel);
    }

    [Route("UpdateProfile")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
    {
        // Şifre alanlarını doğrulamadan muaf tutuyoruz çünkü bu formda onlar yok
        ModelState.Remove("ChangePasswordViewModel");

        if (!ModelState.IsValid) return RedirectToAction(nameof(Index), model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        user.FirstName = model.EditProfileViewModel.FirstName;
        user.LastName = model.EditProfileViewModel.LastName;
        user.PhoneNumber = model.EditProfileViewModel.Phone;
        user.TwoFactorEnabled = model.EditProfileViewModel.IsTwoFactor;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user); // Cookie'yi güncelle
            SetSweetAlertMessage("Başarılı", "Profil bilgileriniz güncellendi.", "success");
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        return RedirectToAction(nameof(Index), model);
    }

    [Route("ResetPassword")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ProfileViewModel model)
    {
        // Profil alanlarını doğrulamadan muaf tutuyoruz
        ModelState.Remove("EditProfileViewModel");

        if (!ModelState.IsValid) return RedirectToAction(nameof(Index), model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var result = await _userManager.ChangePasswordAsync(user,
            model.ChangePasswordViewModel.CurrentPassword,
            model.ChangePasswordViewModel.NewPassword);

        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            SetSweetAlertMessage("Başarılı", "Şifreniz değiştirildi.", "success");
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        return RedirectToAction(nameof(Index), model);
    }
}