using HireHub.Data;
using HireHub.Models.Entities;
using HireHub.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HireHub.Models.Enums;

namespace HireHub.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ============================================
        // DASHBOARD
        // ============================================
        public async Task<IActionResult> Index()
        {
            var totalUsers = await _db.Users.CountAsync();
            var totalRecruiters = (await _userManager.GetUsersInRoleAsync("Recruiter")).Count;
            var totalJobSeekers = (await _userManager.GetUsersInRoleAsync("JobSeeker")).Count;
            var blockedCount = await _db.Users.CountAsync(u => u.IsBlocked);

            var totalCompanies = await _db.Companies.CountAsync();
            var totalJobs = await _db.Jobs.CountAsync();
            var activeJobs = await _db.Jobs.CountAsync(j => j.IsActive);
            var closedJobs = totalJobs - activeJobs;

            var totalApplications = await _db.JobApplications.CountAsync();
            var totalInterviews = await _db.Interviews.CountAsync();

            var appliedCount = await _db.JobApplications.CountAsync(a => a.Status == ApplicationStatus.Applied);
            var shortlistedCount = await _db.JobApplications.CountAsync(a => a.Status == ApplicationStatus.Shortlisted);
            var hiredCount = await _db.JobApplications.CountAsync(a => a.Status == ApplicationStatus.Hired);
            var rejectedCount = await _db.JobApplications.CountAsync(a => a.Status == ApplicationStatus.Rejected);

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalRecruiters = totalRecruiters;
            ViewBag.TotalJobSeekers = totalJobSeekers;
            ViewBag.BlockedCount = blockedCount;
            ViewBag.TotalCompanies = totalCompanies;
            ViewBag.TotalJobs = totalJobs;
            ViewBag.ActiveJobs = activeJobs;
            ViewBag.ClosedJobs = closedJobs;
            ViewBag.TotalApplications = totalApplications;
            ViewBag.TotalInterviews = totalInterviews;
            ViewBag.AppliedCount = appliedCount;
            ViewBag.ShortlistedCount = shortlistedCount;
            ViewBag.HiredCount = hiredCount;
            ViewBag.RejectedCount = rejectedCount;

            ViewBag.RecentJobs = await _db.Jobs
                .Include(j => j.Company)
                .OrderByDescending(j => j.PostedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentApplications = await _db.JobApplications
                .Include(a => a.Job)
                .Include(a => a.JobSeekerProfile)
                    .ThenInclude(p => p.User)
                .OrderByDescending(a => a.AppliedAt)
                .Take(5)
                .ToListAsync();

            return View();
        }

        // ============================================
        // MANAGE USERS
        // ============================================
        public async Task<IActionResult> Users(string? role, string? search, bool? blocked)
        {
            var query = _db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u =>
                    (u.Email != null && u.Email.Contains(search)) ||
                    (u.FullName != null && u.FullName.Contains(search)));
            }

            if (blocked.HasValue)
                query = query.Where(u => u.IsBlocked == blocked.Value);

            var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();

            var userRoles = new Dictionary<string, string>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.FirstOrDefault() ?? "No Role";
            }

            if (!string.IsNullOrWhiteSpace(role))
                users = users.Where(u => userRoles[u.Id] == role).ToList();

            ViewBag.UserRoles = userRoles;
            ViewBag.RoleFilter = role;
            ViewBag.SearchTerm = search;
            ViewBag.BlockedFilter = blocked;

            return View(users);
        }

        public async Task<IActionResult> UserDetails(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Roles = roles;

            var seekerProfile = await _db.JobSeekerProfiles
                .FirstOrDefaultAsync(p => p.UserId == id);

            var recruiterProfile = await _db.RecruiterProfiles
                .Include(r => r.Company)
                .FirstOrDefaultAsync(r => r.UserId == id);

            ViewBag.SeekerProfile = seekerProfile;
            ViewBag.RecruiterProfile = recruiterProfile;

            if (seekerProfile != null)
            {
                ViewBag.ApplicationCount = await _db.JobApplications
                    .CountAsync(a => a.JobSeekerProfileId == seekerProfile.Id);
            }

            if (recruiterProfile?.Company != null)
            {
                ViewBag.JobCount = await _db.Jobs
                    .CountAsync(j => j.CompanyId == recruiterProfile.CompanyId);
            }

            return View(user);
        }

        // POST: /Admin/ToggleBlock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlock(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Prevent blocking own account
            var currentUserId = _userManager.GetUserId(User);
            if (userId == currentUserId)
            {
                TempData["Error"] = "You cannot block your own account.";
                return RedirectToAction(nameof(UserDetails), new { id = userId });
            }

            user.IsBlocked = !user.IsBlocked;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = user.IsBlocked
                ? $"User {user.Email} has been blocked."
                : $"User {user.Email} has been unblocked.";

            return RedirectToAction(nameof(UserDetails), new { id = userId });
        }
        // POST: /Admin/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (userId == currentUserId)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(UserDetails), new { id = userId });
            }

            try
            {
                // ============================
                // 1. Delete seeker profile + all related data
                // ============================
                var seekerProfile = await _db.JobSeekerProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (seekerProfile != null)
                {
                    // Delete interviews → then applications → then saved jobs
                    var apps = await _db.JobApplications
                        .Include(a => a.Interview)
                        .Where(a => a.JobSeekerProfileId == seekerProfile.Id)
                        .ToListAsync();

                    foreach (var app in apps)
                    {
                        if (app.Interview != null)
                            _db.Interviews.Remove(app.Interview);
                    }

                    _db.JobApplications.RemoveRange(apps);

                    var savedJobs = await _db.SavedJobs
                        .Where(s => s.JobSeekerProfileId == seekerProfile.Id)
                        .ToListAsync();
                    _db.SavedJobs.RemoveRange(savedJobs);

                    _db.JobSeekerProfiles.Remove(seekerProfile);
                }

                // ============================
                // 2. Delete recruiter profile + company + jobs
                // ============================
                var recruiterProfile = await _db.RecruiterProfiles
                    .FirstOrDefaultAsync(r => r.UserId == userId);

                if (recruiterProfile != null)
                {
                    if (recruiterProfile.CompanyId.HasValue)
                    {
                        var companyId = recruiterProfile.CompanyId.Value;

                        // Get all jobs of this company
                        var jobs = await _db.Jobs
                            .Where(j => j.CompanyId == companyId)
                            .ToListAsync();

                        foreach (var job in jobs)
                        {
                            var jobApps = await _db.JobApplications
                                .Include(a => a.Interview)
                                .Where(a => a.JobId == job.Id)
                                .ToListAsync();

                            foreach (var app in jobApps)
                            {
                                if (app.Interview != null)
                                    _db.Interviews.Remove(app.Interview);
                            }
                            _db.JobApplications.RemoveRange(jobApps);

                            var savedForJob = await _db.SavedJobs
                                .Where(s => s.JobId == job.Id)
                                .ToListAsync();
                            _db.SavedJobs.RemoveRange(savedForJob);
                        }

                        _db.Jobs.RemoveRange(jobs);

                        // Detach any OTHER recruiters attached to this company
                        var otherRecruiters = await _db.RecruiterProfiles
                            .Where(r => r.CompanyId == companyId && r.UserId != userId)
                            .ToListAsync();
                        foreach (var r in otherRecruiters)
                            r.CompanyId = null;

                        // Delete the company
                        var company = await _db.Companies.FindAsync(companyId);
                        if (company != null)
                            _db.Companies.Remove(company);
                    }

                    _db.RecruiterProfiles.Remove(recruiterProfile);
                }

                // ============================
                // 3. Jobs posted by this user (as recruiter via PostedByUserId)
                // ============================
                // Note: this handles the case where jobs were posted by a user who is
                // NOT the current owner of the company (edge case)
                var postedJobs = await _db.Jobs
                    .Where(j => j.PostedByUserId == userId)
                    .ToListAsync();

                if (postedJobs.Any())
                {
                    foreach (var job in postedJobs)
                    {
                        var jobApps = await _db.JobApplications
                            .Include(a => a.Interview)
                            .Where(a => a.JobId == job.Id)
                            .ToListAsync();

                        foreach (var app in jobApps)
                        {
                            if (app.Interview != null)
                                _db.Interviews.Remove(app.Interview);
                        }
                        _db.JobApplications.RemoveRange(jobApps);

                        var savedForJob = await _db.SavedJobs
                            .Where(s => s.JobId == job.Id)
                            .ToListAsync();
                        _db.SavedJobs.RemoveRange(savedForJob);
                    }
                    _db.Jobs.RemoveRange(postedJobs);
                }

                await _db.SaveChangesAsync();

                // ============================
                // 4. Clean up Identity records (roles, claims, etc.)
                // ============================
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Any())
                    await _userManager.RemoveFromRolesAsync(user, roles);

                var claims = await _userManager.GetClaimsAsync(user);
                if (claims.Any())
                    await _userManager.RemoveClaimsAsync(user, claims);

                var logins = await _userManager.GetLoginsAsync(user);
                foreach (var login in logins)
                    await _userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);

                // ============================
                // 5. Finally delete the user
                // ============================
                var email = user.Email;
                var result = await _userManager.DeleteAsync(user);

                if (!result.Succeeded)
                {
                    TempData["Error"] = "Failed to delete user: " +
                        string.Join(", ", result.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(UserDetails), new { id = userId });
                }

                TempData["Success"] = $"User {email} and all related data deleted.";
                return RedirectToAction(nameof(Users));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Delete failed: " + ex.Message;
                return RedirectToAction(nameof(UserDetails), new { id = userId });
            }
        }

        // ============================================
        // MANAGE JOBS
        // ============================================
        public async Task<IActionResult> Jobs(string? status, string? search)
        {
            var query = _db.Jobs.Include(j => j.Company).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(j =>
                    j.Title.Contains(search) ||
                    (j.Company != null && j.Company.Name.Contains(search)));
            }

            if (status == "active")
                query = query.Where(j => j.IsActive);
            else if (status == "closed")
                query = query.Where(j => !j.IsActive);

            var jobs = await query.OrderByDescending(j => j.PostedAt).ToListAsync();

            var appCounts = new Dictionary<int, int>();
            foreach (var job in jobs)
                appCounts[job.Id] = await _db.JobApplications.CountAsync(a => a.JobId == job.Id);

            ViewBag.AppCounts = appCounts;
            ViewBag.StatusFilter = status;
            ViewBag.SearchTerm = search;

            return View(jobs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleJobStatus(int id)
        {
            var job = await _db.Jobs.FindAsync(id);
            if (job == null) return NotFound();

            job.IsActive = !job.IsActive;
            await _db.SaveChangesAsync();

            TempData["Success"] = job.IsActive ? "Job activated." : "Job closed.";
            return RedirectToAction(nameof(Jobs));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteJob(int id)
        {
            var job = await _db.Jobs
                .Include(j => j.Applications)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null) return NotFound();

            foreach (var app in job.Applications)
            {
                var interview = await _db.Interviews
                    .FirstOrDefaultAsync(i => i.JobApplicationId == app.Id);
                if (interview != null) _db.Interviews.Remove(interview);
            }
            _db.JobApplications.RemoveRange(job.Applications);

            var savedJobs = await _db.SavedJobs.Where(s => s.JobId == id).ToListAsync();
            _db.SavedJobs.RemoveRange(savedJobs);

            _db.Jobs.Remove(job);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Job deleted with all related data.";
            return RedirectToAction(nameof(Jobs));
        }

        // ============================================
        // MANAGE APPLICATIONS
        // ============================================
        // GET: /Admin/Applications
        public async Task<IActionResult> Applications(ApplicationStatus? status)
        {
            var query = _db.JobApplications
                .Include(a => a.Job)
                    .ThenInclude(j => j.Company)
                .Include(a => a.JobSeekerProfile)
                    .ThenInclude(p => p.User)
                .Include(a => a.Interview)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(a => a.Status == status.Value);

            var apps = await query
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            ViewBag.StatusFilter = status;
            return View(apps);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteApplication(int id)
        {
            var app = await _db.JobApplications
                .Include(a => a.Interview)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (app == null) return NotFound();

            if (app.Interview != null)
                _db.Interviews.Remove(app.Interview);

            _db.JobApplications.Remove(app);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Application deleted.";
            return RedirectToAction(nameof(Applications));
        }

        // ============================================
        // MANAGE SUPPORT TICKETS
        // ============================================
        // GET: /Admin/Tickets
        public async Task<IActionResult> Tickets(SupportStatus? status, string? search)
        {
            var query = _db.SupportTickets
                .Include(t => t.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t =>
                    t.Subject.Contains(search) ||
                    (t.User.Email != null && t.User.Email.Contains(search)) ||
                    (t.User.FullName != null && t.User.FullName.Contains(search)));
            }

            if (status.HasValue)
                query = query.Where(t => t.Status == status.Value);

            var tickets = await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            ViewBag.StatusFilter = status;
            ViewBag.SearchTerm = search;
            return View(tickets);
        }

        // GET: /Admin/TicketDetails/5
        public async Task<IActionResult> TicketDetails(int id)
        {
            var ticket = await _db.SupportTickets
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return NotFound();

            return View(ticket);
        }

        // POST: /Admin/ReplyTicket
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyTicket(int id, string adminReply, SupportStatus status)
        {
            var ticket = await _db.SupportTickets.FindAsync(id);
            if (ticket == null) return NotFound();

            ticket.AdminReply = adminReply;
            ticket.RepliedAt = DateTime.UtcNow;
            ticket.Status = status;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Reply sent and status updated.";
            return RedirectToAction(nameof(TicketDetails), new { id });
        }

        // POST: /Admin/DeleteTicket/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            var ticket = await _db.SupportTickets.FindAsync(id);
            if (ticket == null) return NotFound();

            _db.SupportTickets.Remove(ticket);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Ticket deleted.";
            return RedirectToAction(nameof(Tickets));
        }
    }
}