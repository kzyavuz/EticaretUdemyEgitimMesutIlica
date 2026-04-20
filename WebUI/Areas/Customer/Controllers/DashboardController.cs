using Microsoft.AspNetCore.Mvc;

namespace WebUI.Areas.Customer.Controllers
{
    public class DashboardController : CustomerBaseController
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
