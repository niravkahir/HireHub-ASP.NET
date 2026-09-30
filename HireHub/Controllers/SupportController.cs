using HireHub.Data;
using HireHub.Models.Entities;
using HireHub.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HireHub.Controllers
{
    [Authorize(Roles = "JobSeeker,Recruiter")]
    public class SupportController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public SupportController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET: /Support
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var tickets = await _db.SupportTickets
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(tickets);
        }

        // GET: /Support/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Support/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupportTicket model)
        {
            ModelState.Remove("User");
            ModelState.Remove("UserId");

            if (!ModelState.IsValid) return View(model);

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            model.UserId = userId;
            model.Status = SupportStatus.Open;
            model.CreatedAt = DateTime.UtcNow;

            _db.SupportTickets.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Ticket submitted! Our admin will reply soon.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Support/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);

            var ticket = await _db.SupportTickets
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (ticket == null) return NotFound();

            return View(ticket);
        }

        // POST: /Support/Close/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id)
        {
            var userId = _userManager.GetUserId(User);

            var ticket = await _db.SupportTickets
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (ticket == null) return NotFound();

            ticket.Status = SupportStatus.Closed;
            ticket.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Ticket closed.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}