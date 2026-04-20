using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Controllers;

namespace WebUI.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Policy = "UserPolicy")]
    [Route("kullanici-panelim")]
    public class CustomerBaseController : BaseController
    {

    }
}
