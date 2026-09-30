using HireHub.Models.Entities;
using HireHub.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HireHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // ============ REGISTER ============
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                IsBlocked = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                string role = model.Role == "Recruiter" ? "Recruiter" : "JobSeeker";
                await _userManager.AddToRoleAsync(user, role);

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Dashboard");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // ============ LOGIN ============
        [HttpGet]
        public IActionResult Login(string? blocked = null)
        {
            if (blocked == "true")
            {
                ViewBag.BlockedMessage = "Your account has been blocked by the administrator.";
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1) Check if user exists
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            // 2) Check if user is blocked BEFORE attempting sign-in
            if (user.IsBlocked)
            {
                ModelState.AddModelError("", "🚫 Your account has been blocked by the administrator. Please contact support.");
                return View(model);
            }

            // 3) Try password sign-in
            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
                return RedirectToAction("Index", "Dashboard");

            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        // ============ LOGOUT ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ============ ACCESS DENIED ============
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}