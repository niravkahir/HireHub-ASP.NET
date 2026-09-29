using HireHub.Data;
using HireHub.Models.Entities;
using HireHub.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HireHub.Controllers
{
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

        // ============================================
        // HELPERS
        // ============================================

        // Helper: get current job seeker's profile (auto-create if missing)
        private async Task<JobSeekerProfile?> GetMySeekerProfileAsync()
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

        // Helper: get recruiter's company
        private async Task<Company?> GetMyCompanyAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return null;

            var profile = await _db.RecruiterProfiles
                .Include(r => r.Company)
                .FirstOrDefaultAsync(r => r.UserId == userId);

            return profile?.Company;
        }

        // ============================================
        // JOB SEEKER ACTIONS
        // ============================================

        // GET: /Application/Apply/5
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> Apply(int id)
        {
            var job = await _db.Jobs
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == id && j.IsActive);

            if (job == null) return NotFound();

            var profile = await GetMySeekerProfileAsync();
            if (profile == null) return RedirectToAction("Login", "Account");

            // ✅ Check for ACTIVE applications only (withdrawn doesn't block)
            var alreadyApplied = await _db.JobApplications
                .AnyAsync(a => a.JobId == id
                    && a.JobSeekerProfileId == profile.Id
                    && a.Status != ApplicationStatus.Withdrawn);

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
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> Apply(int id, string? coverLetter, IFormFile? resumeFile)
        {
            var job = await _db.Jobs
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == id && j.IsActive);

            if (job == null) return NotFound();

            var profile = await GetMySeekerProfileAsync();
            if (profile == null) return RedirectToAction("Login", "Account");

            // ✅ Check for ACTIVE applications only
            var alreadyApplied = await _db.JobApplications
                .AnyAsync(a => a.JobId == id
                    && a.JobSeekerProfileId == profile.Id
                    && a.Status != ApplicationStatus.Withdrawn);

            if (alreadyApplied)
            {
                TempData["Info"] = "You have already applied to this job.";
                return RedirectToAction("Details", "Browse", new { id });
            }

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
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> MyApplications()
        {
            var profile = await GetMySeekerProfileAsync();
            if (profile == null) return RedirectToAction("Login", "Account");

            // ✅ HIDE WITHDRAWN APPLICATIONS
            var apps = await _db.JobApplications
                .Include(a => a.Job)
                    .ThenInclude(j => j.Company)
                .Include(a => a.Interview)
                .Where(a => a.JobSeekerProfileId == profile.Id
                    && a.Status != ApplicationStatus.Withdrawn)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            return View(apps);
        }

        // POST: /Application/Withdraw/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> Withdraw(int id)
        {
            var profile = await GetMySeekerProfileAsync();
            if (profile == null) return RedirectToAction("Login", "Account");

            var app = await _db.JobApplications
                .FirstOrDefaultAsync(a => a.Id == id && a.JobSeekerProfileId == profile.Id);

            if (app == null) return NotFound();

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

        // ============================================
        // RECRUITER ACTIONS
        // ============================================

        // GET: /Application/JobApplications/5
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> JobApplications(int id)
        {
            var company = await GetMyCompanyAsync();
            if (company == null)
            {
                TempData["Info"] = "Please create your company first.";
                return RedirectToAction("Create", "Company");
            }

            var job = await _db.Jobs
                .FirstOrDefaultAsync(j => j.Id == id && j.CompanyId == company.Id);

            if (job == null) return NotFound();

            // ✅ ONLY SHOW NON-WITHDRAWN APPLICATIONS
            var applications = await _db.JobApplications
                .Include(a => a.JobSeekerProfile)
                    .ThenInclude(p => p.User)
                .Where(a => a.JobId == id
                    && a.Status != ApplicationStatus.Withdrawn)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            ViewBag.Job = job;
            return View(applications);
        }

        // GET: /Application/ApplicantDetails/5
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> ApplicantDetails(int id)
        {
            var company = await GetMyCompanyAsync();
            if (company == null)
            {
                TempData["Info"] = "Please create your company first.";
                return RedirectToAction("Create", "Company");
            }

            // ✅ BLOCK WITHDRAWN APPLICATIONS
            var application = await _db.JobApplications
                .Include(a => a.Job)
                .Include(a => a.JobSeekerProfile)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(a => a.Id == id
                    && a.Job.CompanyId == company.Id
                    && a.Status != ApplicationStatus.Withdrawn);

            if (application == null) return NotFound();

            return View(application);
        }

        // POST: /Application/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> UpdateStatus(int id, ApplicationStatus status)
        {
            var company = await GetMyCompanyAsync();
            if (company == null) return RedirectToAction("Create", "Company");

            // ✅ BLOCK WITHDRAWN
            var application = await _db.JobApplications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == id
                    && a.Job.CompanyId == company.Id
                    && a.Status != ApplicationStatus.Withdrawn);

            if (application == null) return NotFound();

            application.Status = status;
            application.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Status updated to {status}.";
            return RedirectToAction(nameof(ApplicantDetails), new { id });
        }
    }
}