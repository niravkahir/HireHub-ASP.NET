using HireHub.Data;
using HireHub.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HireHub.Controllers
{
    [Authorize(Roles = "Recruiter")]
    public class JobController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public JobController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
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

        // GET: /Job
        public async Task<IActionResult> Index()
        {
            var company = await GetMyCompanyAsync();
            if (company == null)
            {
                TempData["Info"] = "You need to create your company first.";
                return RedirectToAction("Create", "Company");
            }

            var jobs = await _db.Jobs
                .Where(j => j.CompanyId == company.Id)
                .OrderByDescending(j => j.PostedAt)
                .ToListAsync();

            ViewBag.CompanyName = company.Name;
            return View(jobs);
        }

        // GET: /Job/Create
        public async Task<IActionResult> Create()
        {
            var company = await GetMyCompanyAsync();
            if (company == null)
            {
                TempData["Info"] = "You need to create your company first.";
                return RedirectToAction("Create", "Company");
            }
            return View();
        }

        // POST: /Job/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Job model)
        {
            ModelState.Remove("Company");
            ModelState.Remove("PostedBy");
            ModelState.Remove("CompanyId");
            ModelState.Remove("PostedByUserId");

            var company = await GetMyCompanyAsync();
            if (company == null)
            {
                TempData["Info"] = "You need to create your company first.";
                return RedirectToAction("Create", "Company");
            }

            // 🔒 CUSTOM VALIDATION
            if (model.SalaryMin.HasValue && model.SalaryMax.HasValue
                && model.SalaryMin > model.SalaryMax)
            {
                ModelState.AddModelError("SalaryMax", "Max salary must be greater than or equal to min salary.");
            }

            if (model.Deadline.HasValue && model.Deadline.Value.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("Deadline", "Deadline must be a future date.");
            }

            if (!ModelState.IsValid) return View(model);

            model.CompanyId = company.Id;
            model.PostedByUserId = _userManager.GetUserId(User)!;
            model.PostedAt = DateTime.UtcNow;
            model.IsActive = true;

            _db.Jobs.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Job posted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Job/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var company = await GetMyCompanyAsync();
            if (company == null) return RedirectToAction("Create", "Company");

            var job = await _db.Jobs
                .FirstOrDefaultAsync(j => j.Id == id && j.CompanyId == company.Id);

            if (job == null) return NotFound();
            return View(job);
        }

        // POST: /Job/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Job model)
        {
            // ✅ FIX: Remove navigation property validation errors
            ModelState.Remove("Company");
            ModelState.Remove("PostedBy");
            ModelState.Remove("CompanyId");
            ModelState.Remove("PostedByUserId");

            var company = await GetMyCompanyAsync();
            if (company == null) return RedirectToAction("Create", "Company");

            if (id != model.Id) return BadRequest();

            var job = await _db.Jobs
                .FirstOrDefaultAsync(j => j.Id == id && j.CompanyId == company.Id);

            if (job == null) return NotFound();
            if (!ModelState.IsValid) return View(model);

            job.Title = model.Title;
            job.Description = model.Description;
            job.Requirements = model.Requirements;
            job.Location = model.Location;
            job.SalaryMin = model.SalaryMin;
            job.SalaryMax = model.SalaryMax;
            job.JobType = model.JobType;
            job.ExperienceLevel = model.ExperienceLevel;
            job.Deadline = model.Deadline;
            job.IsActive = model.IsActive;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Job updated!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Job/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var company = await GetMyCompanyAsync();
            if (company == null) return RedirectToAction("Create", "Company");

            var job = await _db.Jobs
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == id && j.CompanyId == company.Id);

            if (job == null) return NotFound();
            return View(job);
        }

        // POST: /Job/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var company = await GetMyCompanyAsync();
            if (company == null) return RedirectToAction("Create", "Company");

            var job = await _db.Jobs
                .FirstOrDefaultAsync(j => j.Id == id && j.CompanyId == company.Id);

            if (job == null) return NotFound();

            _db.Jobs.Remove(job);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Job deleted.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Job/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var company = await GetMyCompanyAsync();
            if (company == null) return RedirectToAction("Create", "Company");

            var job = await _db.Jobs
                .FirstOrDefaultAsync(j => j.Id == id && j.CompanyId == company.Id);

            if (job == null) return NotFound();

            job.IsActive = !job.IsActive;
            await _db.SaveChangesAsync();

            TempData["Success"] = job.IsActive ? "Job activated." : "Job closed.";
            return RedirectToAction(nameof(Index));
        }
    }
}