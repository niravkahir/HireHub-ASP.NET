using HireHub.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HireHub.Controllers
{
    [Authorize]     // any logged-in user can access
    public class DashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user!);

            ViewBag.FullName = user?.FullName;
            ViewBag.Email = user?.Email;
            ViewBag.Roles = roles;

            return View();
        }
    }
}