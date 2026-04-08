using Core.Entities;
using Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            return View(data);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var message = await _context.IncomingMessages.FindAsync(id);
            if (message == null) return NotFound();

            if (!message.IsRead)
            {
                message.IsRead = true;
                message.ReadDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }

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
    }
}
