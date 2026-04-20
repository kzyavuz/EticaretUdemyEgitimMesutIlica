using Core.Entities;
using Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUI.Areas.Admin.Models;

namespace WebUI.Areas.Admin.Controllers
{
    public class IncomingMessagesController : AdminBaseController
    {
        private readonly DatabaseContext _context;

        public IncomingMessagesController(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _context.IncomingMessages
                .OrderByDescending(m => m.CreatedDate)
                .ToListAsync();

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Gelen Mesajlar" }
            };
            ViewBag.Breadcrumbs = breadcrumbs;

            return View(data);
        }

        public async Task<IActionResult> Details(int id)
        {
            var message = await _context.IncomingMessages.FindAsync(id);
            if (message == null) return NotFound();

            if (!message.IsRead)
            {
                message.IsRead = true;
                message.ReadDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            List<BreadcrumbItem> breadcrumbs = new()
            {
                new BreadcrumbItem { Title = "Gelen Mesajlar", Controller = "IncomingMessages", Action = "Index" },
                new BreadcrumbItem { Title = message.Subject }
            };

            ViewBag.Breadcrumbs = breadcrumbs;

            return View(message);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var message = await _context.IncomingMessages.FindAsync(id);

            if (message == null)
            {
                return NotFound();

            }
            _context.IncomingMessages.Remove(message);

            await _context.SaveChangesAsync();

            SetSweetAlertMessage("Başarılı", "Mesaj başarıyla silindi.", "success");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                SetSweetAlertMessage("Hata", "Silinecek kayıt seçilmedi.", "error");
                return RedirectToAction(nameof(Index));
            }

            var items = await _context.IncomingMessages.Where(m => ids.Contains(m.Id)).ToListAsync();
            _context.IncomingMessages.RemoveRange(items);
            await _context.SaveChangesAsync();

            SetSweetAlertMessage("Başarılı", $"{items.Count} mesaj silindi.", "success");
            return RedirectToAction(nameof(Index));
        }
    }
}
