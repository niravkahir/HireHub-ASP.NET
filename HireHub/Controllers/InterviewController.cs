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

        // GET: /Interview/Schedule/8   (8 = application ID)
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
            {
                return View(application.Interview);
            }

            // New interview — default tomorrow 10 AM
            return View(new Interview
            {
                JobApplicationId = id,
                ScheduledAt = DateTime.Today.AddDays(1).AddHours(10)
            });
        }

        // POST: /Interview/Schedule/8
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Schedule(int id, Interview model)
        {
            // === DEBUG ===
            Console.WriteLine("===== Interview/Schedule POST =====");
            Console.WriteLine($"Route id = {id}");
            Console.WriteLine($"Model.JobApplicationId = {model.JobApplicationId}");
            Console.WriteLine($"Model.ScheduledAt = {model.ScheduledAt}");
            Console.WriteLine($"ModelState.IsValid = {ModelState.IsValid}");

            foreach (var kv in ModelState)
            {
                foreach (var err in kv.Value.Errors)
                {
                    Console.WriteLine($"  ERROR {kv.Key}: {err.ErrorMessage}");
                }
            }

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

            // Date validation — use local time
            if (model.ScheduledAt < DateTime.Now.AddMinutes(-5))
            {
                ModelState.AddModelError("ScheduledAt", "Interview must be scheduled for a future date/time.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Application = application;
                return View(model);
            }

            if (application.Interview == null)
            {
                var interview = new Interview
                {
                    JobApplicationId = application.Id,
                    ScheduledAt = model.ScheduledAt,
                    Mode = model.Mode,
                    Location = model.Location,
                    Notes = model.Notes,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Interviews.Add(interview);
            }
            else
            {
                application.Interview.ScheduledAt = model.ScheduledAt;
                application.Interview.Mode = model.Mode;
                application.Interview.Location = model.Location;
                application.Interview.Notes = model.Notes;
            }

            application.Status = ApplicationStatus.InterviewScheduled;
            application.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Interview scheduled successfully!";
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

            application.Status = ApplicationStatus.Shortlisted;
            application.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Interview cancelled.";
            return RedirectToAction("ApplicantDetails", "Application", new { id = application.Id });
        }
    }
}