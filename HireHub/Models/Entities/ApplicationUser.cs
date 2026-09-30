using Microsoft.AspNetCore.Identity;

namespace HireHub.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsBlocked { get; set; } = false;

        // Navigation
        public JobSeekerProfile? JobSeekerProfile { get; set; }
        public RecruiterProfile? RecruiterProfile { get; set; }
    }
}