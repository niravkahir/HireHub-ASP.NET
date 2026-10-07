using HireHub.Data;
using HireHub.Models.Entities;
using HireHub.Models.Enums;
using HireHub.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HireHub.Controllers
{
    // Public controller — anyone can browse jobs
    public class BrowseController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public BrowseController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET: /Browse
        public async Task<IActionResult> Index(string? keyword, string? location,
            JobType? jobType, ExperienceLevel? experienceLevel, int page = 1)
        {
            const int pageSize = 5;

            var query = _db.Jobs
                .Include(j => j.Company)
                .Where(j => j.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(j =>
                    j.Title.Contains(keyword) ||
                    j.Description.Contains(keyword) ||
                    (j.Requirements != null && j.Requirements.Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(j => j.Location != null && j.Location.Contains(location));
            }

            if (jobType.HasValue)
                query = query.Where(j => j.JobType == jobType.Value);

            if (experienceLevel.HasValue)
                query = query.Where(j => j.ExperienceLevel == experienceLevel.Value);

            var totalJobs = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalJobs / (double)pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var jobs = await query
                .OrderByDescending(j => j.PostedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new JobSearchViewModel
            {
                Keyword = keyword,
                Location = location,
                JobType = jobType,
                ExperienceLevel = experienceLevel,
                Results = jobs,
                CurrentPage = page,
                TotalJobs = totalJobs,
                PageSize = pageSize
            };

            ViewBag.JobTypes = new SelectList(Enum.GetValues(typeof(JobType)), jobType);
            ViewBag.ExperienceLevels = new SelectList(Enum.GetValues(typeof(ExperienceLevel)), experienceLevel);

            return View(vm);
        }

        // GET: /Browse/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var job = await _db.Jobs
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == id && j.IsActive);

            if (job == null) return NotFound();

            bool alreadyApplied = false;
            bool isJobSeeker = User.Identity != null
                               && User.Identity.IsAuthenticated
                               && User.IsInRole("JobSeeker");

            bool isSaved = false;

            if (isJobSeeker)
            {
                var userId = _userManager.GetUserId(User);
                if (!string.IsNullOrEmpty(userId))
                {
                    var profile = await _db.JobSeekerProfiles
                        .FirstOrDefaultAsync(p => p.UserId == userId);

                    if (profile != null)
                    {
                        alreadyApplied = await _db.JobApplications
                            .AnyAsync(a => a.JobId == id && a.JobSeekerProfileId == profile.Id);

                        isSaved = await _db.SavedJobs
                            .AnyAsync(s => s.JobId == id && s.JobSeekerProfileId == profile.Id);
                    }
                }
            }

            bool isExpired = job.Deadline.HasValue
                             && job.Deadline.Value.Date < DateTime.UtcNow.Date;

            ViewBag.AlreadyApplied = alreadyApplied;
            ViewBag.IsJobSeeker = isJobSeeker;
            ViewBag.IsExpired = isExpired;
            ViewBag.IsSaved = isSaved;

            return View(job);
        }
    }
}