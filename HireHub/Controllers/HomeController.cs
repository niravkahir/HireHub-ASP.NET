using Microsoft.AspNetCore.Mvc;

namespace HireHub.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}