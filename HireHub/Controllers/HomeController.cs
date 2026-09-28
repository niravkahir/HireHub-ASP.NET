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
            // Logged in users → go straight to Dashboard
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}