using HireHub.Data;
using HireHub.Models.Entities;
using HireHub.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HireHub.Controllers
{
    [Authorize(Roles = "JobSeeker")]
    public class ApplicationController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ApplicationController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _env = env;
        }

        // Helper: get current job seeker's profile (auto-create if missing)
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

        // GET: /Application/Apply/5
        public async Task<IActionResult> Apply(int id)
        {
            var job = await _db.Jobs
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == id && j.IsActive);

            if (job == null) return NotFound();

            var profile = await GetMyProfileAsync();
            if (profile == null) return RedirectToAction("Login", "Account");

            // Check for duplicate
            var alreadyApplied = await _db.JobApplications
                .AnyAsync(a => a.JobId == id && a.JobSeekerProfileId == profile.Id);

            if (alreadyApplied)
            {
                TempData["Info"] = "You have already applied to this job.";
                return RedirectToAction("Details", "Browse", new { id });
            }

            ViewBag.Job = job;
            return View();
        }

        // POST: /Application/Apply/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int id, string? coverLetter, IFormFile? resumeFile)
        {
            var job = await _db.Jobs
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == id && j.IsActive);

            if (job == null) return NotFound();

            var profile = await GetMyProfileAsync();
            if (profile == null) return RedirectToAction("Login", "Account");

            // Duplicate check again
            var alreadyApplied = await _db.JobApplications
                .AnyAsync(a => a.JobId == id && a.JobSeekerProfileId == profile.Id);

            if (alreadyApplied)
            {
                TempData["Info"] = "You have already applied to this job.";
                return RedirectToAction("Details", "Browse", new { id });
            }

            // Handle resume upload
            string? resumePath = null;
            if (resumeFile != null && resumeFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(resumeFile.FileName);
                var folder = Path.Combine(_env.WebRootPath, "uploads", "resumes");
                Directory.CreateDirectory(folder);
                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await resumeFile.CopyToAsync(stream);
                }

                resumePath = "/uploads/resumes/" + fileName;
            }

            var application = new JobApplication
            {
                JobId = job.Id,
                JobSeekerProfileId = profile.Id,
                CoverLetter = coverLetter,
                ResumePath = resumePath,
                Status = ApplicationStatus.Applied,
                AppliedAt = DateTime.UtcNow
            };

            _db.JobApplications.Add(application);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Successfully applied to {job.Title}!";
            return RedirectToAction(nameof(MyApplications));
        }

        // GET: /Application/MyApplications
        public async Task<IActionResult> MyApplications()
        {
            var profile = await GetMyProfileAsync();
            if (profile == null) return RedirectToAction("Login", "Account");

            var apps = await _db.JobApplications
                .Include(a => a.Job)
                    .ThenInclude(j => j.Company)
                .Where(a => a.JobSeekerProfileId == profile.Id)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            return View(apps);
        }

        // POST: /Application/Withdraw/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(int id)
        {
            var profile = await GetMyProfileAsync();
            if (profile == null) return RedirectToAction("Login", "Account");

            var app = await _db.JobApplications
                .FirstOrDefaultAsync(a => a.Id == id && a.JobSeekerProfileId == profile.Id);

            if (app == null) return NotFound();

            // Only allow withdraw if not yet hired / rejected
            if (app.Status == ApplicationStatus.Hired ||
                app.Status == ApplicationStatus.Rejected)
            {
                TempData["Error"] = "You cannot withdraw this application.";
                return RedirectToAction(nameof(MyApplications));
            }

            app.Status = ApplicationStatus.Withdrawn;
            app.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Application withdrawn.";
            return RedirectToAction(nameof(MyApplications));
        }
    }
}