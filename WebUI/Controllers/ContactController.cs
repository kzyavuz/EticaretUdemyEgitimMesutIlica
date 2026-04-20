using Core.Entities;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;
using Service.Service;

namespace WebUI.Controllers
{
    public class ContactController : BaseController
    {
        private readonly IContactService _contactService;
        private readonly IIncomingMessageService _incomingMessageService;

        public ContactController(IContactService contactService, IIncomingMessageService incomingMessageService)
        {
            _contactService = contactService;
            _incomingMessageService = incomingMessageService;
        }


        public async Task<IActionResult> Index()
        {
            var contact = await _contactService.GetListAsync();

            ContactViewModel data = new()
            {
                IncomingMessage = new IncomingMessage(),
                Contacts = contact
            };

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind(Prefix = "IncomingMessage")] IncomingMessage model)
        {
            if (!ModelState.IsValid)
            {
                SetSweetAlertMessage("Hata", "Lütfen formu doğru şekilde doldurunuz.", "error");
                return View(nameof(Index));
            }

            try
            {
                model.IsRead = false;
                model.ReadDate = null;
                await _incomingMessageService.CreateAsync(model);
                SetSweetAlertMessage("Başarılı", "Mesajınız başarıyla gönderildi!", "success");
                return RedirectToAction(nameof(Index));

            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework like Serilog, NLog, etc.)
                // For simplicity, we're just writing to the console here.
                Console.WriteLine($"Error sending message: {ex.Message}");
                SetSweetAlertMessage("Hata", "Mesaj gönderilirken bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.", "error");
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
