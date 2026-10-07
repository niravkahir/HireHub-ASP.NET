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

        private async Task<Company?> GetMyCompanyAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return null;

            var profile = await _db.RecruiterProfiles
                .Include(r => r.Company)
                .FirstOrDefaultAsync(r => r.UserId == userId);

            return profile?.Company;
        }


        // GET: /Application/Apply/5
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> Apply(int id)
        {
            var job = await _db.Jobs
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == id && j.IsActive);

            if (job == null) return NotFound();

            if (job.Deadline.HasValue && job.Deadline.Value.Date < DateTime.UtcNow.Date)
            {
                TempData["Error"] = "The application deadline for this job has passed.";
                return RedirectToAction("Details", "Browse", new { id });
            }

            var profile = await GetMyProfileAsync();
            if (profile == null) return RedirectToAction("Login", "Account");

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

            if (job.Deadline.HasValue && job.Deadline.Value.Date < DateTime.UtcNow.Date)
            {
                TempData["Error"] = "The application deadline for this job has passed.";
                return RedirectToAction("Details", "Browse", new { id });
            }

            var profile = await GetMyProfileAsync();
            if (profile == null) return RedirectToAction("Login", "Account");

            var alreadyApplied = await _db.JobApplications
                .AnyAsync(a => a.JobId == id && a.JobSeekerProfileId == profile.Id);

            if (alreadyApplied)
            {
                TempData["Info"] = "You have already applied to this job.";
                return RedirectToAction("Details", "Browse", new { id });
            }

            if (resumeFile == null || resumeFile.Length == 0)
            {
                ViewBag.Job = job;
                ViewBag.ResumeError = "Please upload your resume before submitting.";
                return View();
            }

            if (resumeFile.Length > 5 * 1024 * 1024)
            {
                ViewBag.Job = job;
                ViewBag.ResumeError = "Resume file size must be under 5 MB.";
                return View();
            }

            var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
            var ext = Path.GetExtension(resumeFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                ViewBag.Job = job;
                ViewBag.ResumeError = "Only PDF, DOC, or DOCX files are allowed.";
                return View();
            }

            var fileName = Guid.NewGuid() + ext;
            var folder = Path.Combine(_env.WebRootPath, "uploads", "resumes");
            Directory.CreateDirectory(folder);
            var filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await resumeFile.CopyToAsync(stream);
            }

            var resumePath = "/uploads/resumes/" + fileName;

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
            var profile = await GetMyProfileAsync();
            if (profile == null) return RedirectToAction("Login", "Account");

            var apps = await _db.JobApplications
                .Include(a => a.Job)
                    .ThenInclude(j => j.Company)
                .Include(a => a.Interview)
                .Where(a => a.JobSeekerProfileId == profile.Id)
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
            var profile = await GetMyProfileAsync();
            if (profile == null) return RedirectToAction("Login", "Account");

            var app = await _db.JobApplications
                .Include(a => a.Interview)
                .FirstOrDefaultAsync(a => a.Id == id && a.JobSeekerProfileId == profile.Id);

            if (app == null) return NotFound();

            if (app.Status == ApplicationStatus.Hired ||
                app.Status == ApplicationStatus.Rejected)
            {
                TempData["Error"] = "You cannot withdraw a final application.";
                return RedirectToAction(nameof(MyApplications));
            }

            if (app.Interview != null)
            {
                _db.Interviews.Remove(app.Interview);
            }

            _db.JobApplications.Remove(app);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Application withdrawn successfully.";
            return RedirectToAction(nameof(MyApplications));
        }

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

            var applications = await _db.JobApplications
                .Include(a => a.JobSeekerProfile)
                    .ThenInclude(p => p.User)
                .Include(a => a.Interview)
                .Where(a => a.JobId == id)
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

            var application = await _db.JobApplications
                .Include(a => a.Job)
                .Include(a => a.JobSeekerProfile)
                    .ThenInclude(p => p.User)
                .Include(a => a.Interview)
                .FirstOrDefaultAsync(a => a.Id == id && a.Job.CompanyId == company.Id);

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

            var application = await _db.JobApplications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == id && a.Job.CompanyId == company.Id);

            if (application == null) return NotFound();

            if (application.Status == ApplicationStatus.Hired ||
                application.Status == ApplicationStatus.Rejected ||
                application.Status == ApplicationStatus.Withdrawn)
            {
                TempData["Error"] = $"This application is already '{application.Status}' and cannot be changed.";
                return RedirectToAction(nameof(ApplicantDetails), new { id });
            }

            // Prevent manual InterviewScheduled — use Schedule Interview button
            if (status == ApplicationStatus.InterviewScheduled)
            {
                TempData["Error"] = "Use 'Schedule Interview' to set this status.";
                return RedirectToAction(nameof(ApplicantDetails), new { id });
            }

            // Prevent setting to Hired unless currently InterviewScheduled
            if (status == ApplicationStatus.Hired &&
                application.Status != ApplicationStatus.InterviewScheduled)
            {
                TempData["Error"] = "You can only hire a candidate after scheduling an interview.";
                return RedirectToAction(nameof(ApplicantDetails), new { id });
            }

            application.Status = status;
            application.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Status updated to {status}.";
            return RedirectToAction(nameof(ApplicantDetails), new { id });
        }
    }
}