using HireHub.Data;
using HireHub.Models.Entities;
using HireHub.Models.Enums;
using HireHub.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HireHub.Controllers
{
    // Public controller — anyone can browse jobs
    public class BrowseController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BrowseController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Browse
        public async Task<IActionResult> Index(string? keyword, string? location,
            JobType? jobType, ExperienceLevel? experienceLevel)
        {
            var query = _db.Jobs
                .Include(j => j.Company)
                .Where(j => j.IsActive)  // only show active jobs
                .AsQueryable();

            // Apply filters
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
            {
                query = query.Where(j => j.JobType == jobType.Value);
            }

            if (experienceLevel.HasValue)
            {
                query = query.Where(j => j.ExperienceLevel == experienceLevel.Value);
            }

            var jobs = await query
                .OrderByDescending(j => j.PostedAt)
                .ToListAsync();

            // Build ViewModel
            var vm = new JobSearchViewModel
            {
                Keyword = keyword,
                Location = location,
                JobType = jobType,
                ExperienceLevel = experienceLevel,
                Results = jobs
            };

            // For dropdowns
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

            return View(job);
        }
    }
}