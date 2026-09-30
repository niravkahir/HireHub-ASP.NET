using HireHub.Data;
using HireHub.Models.Entities;
using HireHub.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HireHub.Controllers
{
    [Authorize(Roles = "Recruiter")]
    public class InterviewController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public InterviewController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
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

        // GET: /Interview/Schedule/8
        public async Task<IActionResult> Schedule(int id)
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

            ViewBag.Application = application;

            if (application.Interview != null)
                return View(application.Interview);

            return View(new Interview
            {
                JobApplicationId = id,
                ScheduledAt = DateTime.Today.AddDays(1).AddHours(10),
                Mode = "Online"
            });
        }

        // POST: /Interview/Schedule/8
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Schedule(int id, Interview model)
        {
            // Remove validation errors for navigation properties
            ModelState.Remove("JobApplication");
            ModelState.Remove("CreatedAt");

            var company = await GetMyCompanyAsync();
            if (company == null) return RedirectToAction("Create", "Company");

            var application = await _db.JobApplications
                .Include(a => a.Job)
                .Include(a => a.JobSeekerProfile)
                    .ThenInclude(p => p.User)
                .Include(a => a.Interview)
                .FirstOrDefaultAsync(a => a.Id == id && a.Job.CompanyId == company.Id);

            if (application == null) return NotFound();

            // Future date check
            if (model.ScheduledAt < DateTime.Now.AddMinutes(-5))
            {
                ModelState.AddModelError("ScheduledAt", "Interview must be scheduled for a future date/time.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Application = application;
                return View(model);
            }

            // ============================================
            // CREATE OR UPDATE INTERVIEW
            // ============================================
            if (application.Interview == null)
            {
                // CASE 1: No interview yet → create new
                var interview = new Interview
                {
                    JobApplicationId = application.Id,
                    ScheduledAt = model.ScheduledAt,
                    Mode = string.IsNullOrEmpty(model.Mode) ? "Online" : model.Mode,
                    Location = model.Location,
                    Notes = model.Notes,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Interviews.Add(interview);
            }
            else
            {
                // CASE 2: Interview already exists → update it
                application.Interview.ScheduledAt = model.ScheduledAt;
                application.Interview.Mode = string.IsNullOrEmpty(model.Mode) ? "Online" : model.Mode;
                application.Interview.Location = model.Location;
                application.Interview.Notes = model.Notes;
            }

            // ============================================
            // AUTO-UPDATE STATUS
            // ============================================
            application.Status = ApplicationStatus.InterviewScheduled;
            application.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Interview scheduled! Status updated to 'Interview Scheduled'.";
            return RedirectToAction("ApplicantDetails", "Application", new { id = application.Id });
        }

        // POST: /Interview/Cancel/8
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var company = await GetMyCompanyAsync();
            if (company == null) return RedirectToAction("Create", "Company");

            var application = await _db.JobApplications
                .Include(a => a.Job)
                .Include(a => a.Interview)
                .FirstOrDefaultAsync(a => a.Id == id && a.Job.CompanyId == company.Id);

            if (application == null) return NotFound();
            if (application.Interview == null) return NotFound();

            _db.Interviews.Remove(application.Interview);

            // Reset status to Shortlisted
            application.Status = ApplicationStatus.Shortlisted;
            application.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Interview cancelled. Status reverted to 'Shortlisted'.";
            return RedirectToAction("ApplicantDetails", "Application", new { id = application.Id });
        }
    }
}