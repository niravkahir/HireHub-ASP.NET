using HireHub.Data;
using HireHub.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HireHub.Controllers
{
    [Authorize(Roles = "JobSeeker")]
    public class SavedJobController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public SavedJobController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // Helper: get seeker profile (auto-create if missing)
        private async Task<JobSeekerProfile?> GetMyProfileAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return null;

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

        // GET: /SavedJob
        public async Task<IActionResult> Index()
        {
            var profile = await GetMyProfileAsync();
            if (profile == null) return RedirectToAction("Login", "Account");

            var savedJobs = await _db.SavedJobs
                .Include(s => s.Job)
                    .ThenInclude(j => j.Company)
                .Where(s => s.JobSeekerProfileId == profile.Id)
                .OrderByDescending(s => s.SavedAt)
                .ToListAsync();

            return View(savedJobs);
        }

        // POST: /SavedJob/Save/5  (5 = job ID)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(int id)
        {
            var job = await _db.Jobs
                .FirstOrDefaultAsync(j => j.Id == id && j.IsActive);

            if (job == null) return NotFound();

            var profile = await GetMyProfileAsync();
            if (profile == null) return RedirectToAction("Login", "Account");

            // Check if already saved
            var existing = await _db.SavedJobs
                .FirstOrDefaultAsync(s => s.JobId == id && s.JobSeekerProfileId == profile.Id);

            if (existing == null)
            {
                var savedJob = new SavedJob
                {
                    JobId = id,
                    JobSeekerProfileId = profile.Id,
                    SavedAt = DateTime.UtcNow
                };
                _db.SavedJobs.Add(savedJob);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Job saved to your list!";
            }
            else
            {
                TempData["Info"] = "Job already in your saved list.";
            }

            return RedirectToAction("Details", "Browse", new { id });
        }

        // POST: /SavedJob/Unsave/5  (5 = job ID)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unsave(int id)
        {
            var profile = await GetMyProfileAsync();
            if (profile == null) return RedirectToAction("Login", "Account");

            var savedJob = await _db.SavedJobs
                .FirstOrDefaultAsync(s => s.JobId == id && s.JobSeekerProfileId == profile.Id);

            if (savedJob != null)
            {
                _db.SavedJobs.Remove(savedJob);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Job removed from saved list.";
            }

            return RedirectToAction("Details", "Browse", new { id });
        }

        // POST: /SavedJob/UnsaveFromList/5  (5 = savedJob ID)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnsaveFromList(int id)
        {
            var profile = await GetMyProfileAsync();
            if (profile == null) return RedirectToAction("Login", "Account");

            var savedJob = await _db.SavedJobs
                .FirstOrDefaultAsync(s => s.Id == id && s.JobSeekerProfileId == profile.Id);

            if (savedJob != null)
            {
                _db.SavedJobs.Remove(savedJob);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Job removed from saved list.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}