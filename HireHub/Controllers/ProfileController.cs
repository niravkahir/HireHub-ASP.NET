using HireHub.Data;
using HireHub.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HireHub.Controllers
{
    [Authorize(Roles = "JobSeeker")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // Helper: get or create profile
        private async Task<JobSeekerProfile> GetOrCreateProfileAsync()
        {
            var userId = _userManager.GetUserId(User)!;
            var profile = await _db.JobSeekerProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                profile = new JobSeekerProfile { UserId = userId };
                _db.JobSeekerProfiles.Add(profile);
                await _db.SaveChangesAsync();
            }

            return profile;
        }

        // GET: /Profile
        public async Task<IActionResult> Index()
        {
            var profile = await GetOrCreateProfileAsync();
            var user = await _userManager.GetUserAsync(User);

            ViewBag.FullName = user?.FullName;
            ViewBag.Email = user?.Email;
            ViewBag.PhoneNumber = user?.PhoneNumber;

            return View(profile);
        }

        // POST: /Profile/Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(JobSeekerProfile model)
        {
            // Remove navigation properties from validation
            ModelState.Remove("User");
            ModelState.Remove("UserId");
            ModelState.Remove("Applications");
            ModelState.Remove("SavedJobs");

            var profile = await GetOrCreateProfileAsync();

            if (!ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                ViewBag.FullName = user?.FullName;
                ViewBag.Email = user?.Email;
                ViewBag.PhoneNumber = user?.PhoneNumber;
                return View(model);
            }

            profile.Phone = model.Phone;
            profile.Location = model.Location;
            profile.Skills = model.Skills;
            profile.Education = model.Education;
            profile.Experience = model.Experience;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}