using Core.Entities;
using Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Service.Extensions;
using WebUI.Areas.Admin.Models;

namespace WebUI.Areas.Admin.Controllers
{
    public class AdressesController : AdminBaseController
    {
        private readonly DatabaseContext _context;

        public AdressesController(DatabaseContext context)
        {
            _context = context;
        }

        // GET: Admin/Adresses
        public async Task<IActionResult> Index()
        {
            var data = await _context.Adresses.Include(a => a.AppUser).OrderByDescending(b => b.CreatedDate).ToListAsync();

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Adresler"}
            };

            List<StartCardModel> startCards = new()
            {
                new StartCardModel
                {
                    Title = "Tüm Adresler",
                    Value = data.Count,
                    Class = "info",
                    Tooltip = "Sitede bulunan toplam adres sayısı",
                    Icon = "fa-solid fa-list"
                },
                new StartCardModel
                {
                    Title = "Aktif Adresler",
                    Value = data.Count(p => FunctionHelper.IsPublic(p.Status)),
                    Class = "success",
                    Tooltip = "Sitede aktif durumda olan adres sayısı",
                    Icon = "fa-solid fa-check"
                },
                new StartCardModel
                {
                    Title = "Taslak Adresler",
                    Value = data.Count(p =>  FunctionHelper.IsDraft(p.Status)),
                    Class = "secondary",
                    Tooltip = "Sitede taslak durumda olan adres sayısı",
                    Icon = "fa-solid fa-file"
                }
            };

            ViewBag.Breadcrumbs = breadcrumbs;
            ViewBag.StartCards = startCards;

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            Adress? data = await _context.Adresses.Include(a => a.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (data == null)
            {
                return NotFound();
            }

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Adresler", Controller= "Adresses", Action = "Index" },
                new BreadcrumbItem { Title = data.Title }
            };

            ViewBag.Breadcrumbs = breadcrumbs;

            return View(data);
        }

        public async Task<IActionResult> Form(int? id)
        {
            Adress? data = new();

            if (id.HasValue)
            {
                data = await _context.Adresses.FindAsync(id.Value);
                if (data == null)
                {
                    return NotFound();
                }
            }

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Adresler", Controller= "Adresses", Action = "Index" },
                new BreadcrumbItem { Title = data?.Title ?? "Yeni Adres" }
            };

            ViewBag.Breadcrumbs = breadcrumbs;

            ViewData["AppuserId"] = new SelectList(_context.Users, "Id", "Email", data?.AppuserId);
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Adress adress)
        {
            if (ModelState.IsValid)
            {
                _context.Add(adress);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AppuserId"] = new SelectList(_context.Users, "Id", "Email", adress.AppuserId);
            return View(adress);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Adress adress)
        {
            if (id != adress.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(adress);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AdressExists(adress.Id))
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
            ViewData["AppuserId"] = new SelectList(_context.Users, "Id", "Email", adress.AppuserId);
            return View(adress);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var adress = await _context.Adresses.FindAsync(id);
            if (adress != null)
            {
                _context.Adresses.Remove(adress);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AdressExists(int id)
        {
            return _context.Adresses.Any(e => e.Id == id);
        }
    }
}
