using HireHub.Models.Enums;

namespace HireHub.Models.Entities
{
    public class Job
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Requirements { get; set; }
        public string? Location { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public JobType JobType { get; set; }
        public ExperienceLevel ExperienceLevel { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime PostedAt { get; set; } 
        public DateTime? Deadline { get; set; }

        public int CompanyId { get; set; }
        public string PostedByUserId { get; set; } = string.Empty;

        public Company Company { get; set; } = null!;
        public ApplicationUser PostedBy { get; set; } = null!;
        public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
        public ICollection<SavedJob> SavedByUsers { get; set; } = new List<SavedJob>();
    }
}