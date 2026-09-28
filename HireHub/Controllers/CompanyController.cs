using HireHub.Data;
using HireHub.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HireHub.Controllers
{
    [Authorize(Roles = "Recruiter")]
    public class CompanyController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public CompanyController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _env = env;
        }

        // GET: /Company
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            Console.WriteLine($"===== Company/Index =====");
            Console.WriteLine($"userId = {userId ?? "NULL"}");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var recruiterProfile = await _db.RecruiterProfiles
                .Include(r => r.Company)
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiterProfile == null)
            {
                Console.WriteLine("Profile NULL → creating new");
                recruiterProfile = new RecruiterProfile { UserId = userId };
                _db.RecruiterProfiles.Add(recruiterProfile);
                await _db.SaveChangesAsync();
                Console.WriteLine($"Created profile Id = {recruiterProfile.Id}");
            }
            else
            {
                Console.WriteLine($"Profile found Id = {recruiterProfile.Id}, CompanyId = {recruiterProfile.CompanyId}");
            }

            if (recruiterProfile.CompanyId == null)
            {
                Console.WriteLine("No company → redirect to Create");
                return RedirectToAction(nameof(Create));
            }

            return View(recruiterProfile.Company);
        }

        // GET: /Company/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Company/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Company model, IFormFile? logoFile)
        {
            Console.WriteLine($"===== Company/Create POST =====");
            Console.WriteLine($"ModelState.IsValid = {ModelState.IsValid}");

            if (!ModelState.IsValid) return View(model);

            model.CreatedAt = DateTime.UtcNow;

            if (logoFile != null && logoFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(logoFile.FileName);
                var folder = Path.Combine(_env.WebRootPath, "uploads", "logos");
                Directory.CreateDirectory(folder);
                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }

                model.LogoPath = "/uploads/logos/" + fileName;
            }

            _db.Companies.Add(model);
            await _db.SaveChangesAsync();
            Console.WriteLine($"Company saved Id={model.Id}");

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var profile = await _db.RecruiterProfiles
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (profile == null)
            {
                Console.WriteLine("Profile NULL → creating new");
                profile = new RecruiterProfile
                {
                    UserId = userId,
                    CompanyId = model.Id
                };
                _db.RecruiterProfiles.Add(profile);
            }
            else
            {
                profile.CompanyId = model.Id;
            }

            await _db.SaveChangesAsync();
            Console.WriteLine($"Linked profile to CompanyId={model.Id}");

            return RedirectToAction(nameof(Index));
        }

        // GET: /Company/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var company = await _db.Companies.FindAsync(id);
            if (company == null) return NotFound();
            return View(company);
        }

        // POST: /Company/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Company model, IFormFile? logoFile)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var company = await _db.Companies.FindAsync(id);
            if (company == null) return NotFound();

            company.Name = model.Name;
            company.Description = model.Description;
            company.Website = model.Website;
            company.Location = model.Location;

            if (company.CreatedAt.Year < 2000)
                company.CreatedAt = DateTime.UtcNow;

            if (logoFile != null && logoFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(logoFile.FileName);
                var folder = Path.Combine(_env.WebRootPath, "uploads", "logos");
                Directory.CreateDirectory(folder);
                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }

                company.LogoPath = "/uploads/logos/" + fileName;
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}