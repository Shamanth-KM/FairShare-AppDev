using Microsoft.AspNetCore.Mvc;

namespace FairShare.Web.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
