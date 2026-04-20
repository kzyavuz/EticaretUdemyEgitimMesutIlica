using Microsoft.AspNetCore.Mvc;

namespace WebUI.Areas.Customer.Controllers
{
    public class CheckoutController : CustomerBaseController
    {
        [HttpGet("odeme")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
