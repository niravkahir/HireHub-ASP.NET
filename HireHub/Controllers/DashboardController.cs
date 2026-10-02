using HireHub.Data;
using HireHub.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HireHub.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email;
            ViewBag.Phone = user.PhoneNumber;
            ViewBag.Roles = roles;

            if (roles.Contains("Recruiter"))
            {
                // Recruiter stats
                var profile = await _db.RecruiterProfiles
                    .Include(r => r.Company)
                    .FirstOrDefaultAsync(r => r.UserId == user.Id);

                ViewBag.HasCompany = profile?.CompanyId != null;
                ViewBag.CompanyName = profile?.Company?.Name;

                if (profile?.CompanyId != null)
                {
                    ViewBag.TotalJobs = await _db.Jobs
                        .CountAsync(j => j.CompanyId == profile.CompanyId);

                    ViewBag.ActiveJobs = await _db.Jobs
                        .CountAsync(j => j.CompanyId == profile.CompanyId && j.IsActive);

                    var jobIds = await _db.Jobs
                        .Where(j => j.CompanyId == profile.CompanyId)
                        .Select(j => j.Id)
                        .ToListAsync();

                    ViewBag.TotalApplications = await _db.JobApplications
                        .CountAsync(a => jobIds.Contains(a.JobId));

                    ViewBag.TotalInterviews = await _db.JobApplications
                        .Include(a => a.Interview)
                        .CountAsync(a => jobIds.Contains(a.JobId) && a.Interview != null);

                    // Recent applicants
                    ViewBag.RecentApplications = await _db.JobApplications
                        .Include(a => a.Job)
                        .Include(a => a.JobSeekerProfile)
                            .ThenInclude(p => p.User)
                        .Where(a => jobIds.Contains(a.JobId))
                        .OrderByDescending(a => a.AppliedAt)
                        .Take(5)
                        .ToListAsync();
                }
            }
            else if (roles.Contains("JobSeeker"))
            {
                // JobSeeker stats
                var profile = await _db.JobSeekerProfiles
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);

                if (profile != null)
                {
                    ViewBag.TotalApplications = await _db.JobApplications
                        .CountAsync(a => a.JobSeekerProfileId == profile.Id);

                    ViewBag.ShortlistedCount = await _db.JobApplications
                        .CountAsync(a => a.JobSeekerProfileId == profile.Id
                            && a.Status == Models.Enums.ApplicationStatus.Shortlisted);

                    ViewBag.InterviewsCount = await _db.JobApplications
                        .Include(a => a.Interview)
                        .CountAsync(a => a.JobSeekerProfileId == profile.Id && a.Interview != null);

                    ViewBag.SavedCount = await _db.SavedJobs
                        .CountAsync(s => s.JobSeekerProfileId == profile.Id);

                    ViewBag.HiredCount = await _db.JobApplications
                        .CountAsync(a => a.JobSeekerProfileId == profile.Id
                            && a.Status == Models.Enums.ApplicationStatus.Hired);

                    // Recent applications
                    ViewBag.RecentApplications = await _db.JobApplications
                        .Include(a => a.Job)
                            .ThenInclude(j => j.Company)
                        .Where(a => a.JobSeekerProfileId == profile.Id)
                        .OrderByDescending(a => a.AppliedAt)
                        .Take(5)
                        .ToListAsync();
                }

                ViewBag.ProfileIncomplete = profile == null
                    || string.IsNullOrEmpty(profile.Skills)
                    || string.IsNullOrEmpty(profile.Location);
            }
            else if (roles.Contains("Admin"))
            {
                ViewBag.TotalUsers = await _db.Users.CountAsync();
                ViewBag.TotalJobs = await _db.Jobs.CountAsync();
                ViewBag.TotalApplications = await _db.JobApplications.CountAsync();
                ViewBag.TotalTickets = await _db.SupportTickets.CountAsync();
            }

            return View();
        }
    }
}