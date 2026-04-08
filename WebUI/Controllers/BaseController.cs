using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    public class BaseController : Controller
    {
        protected void SetSweetAlertMessage(string title, string message, string icon = "info")
        {
            TempData["Swal_Title"] = title;
            TempData["Swal_Message"] = message;
            TempData["Swal_Icon"] = icon;
        }
    }
}
