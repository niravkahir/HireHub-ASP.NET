using HireHub.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HireHub.Controllers
{
    public class HomeController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public HomeController(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public IActionResult Index()
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Dashboard");

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // 🔴 500 Error Page
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }

        // 🔴 404 / other status code page
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult StatusCode(int code = 404)
        {
            ViewBag.Code = code;

            if (code == 404)
                ViewBag.Message = "The page you're looking for doesn't exist.";
            else if (code == 403)
                ViewBag.Message = "You don't have permission to access this page.";
            else
                ViewBag.Message = "Something went wrong.";

            return View();
        }
    }
}