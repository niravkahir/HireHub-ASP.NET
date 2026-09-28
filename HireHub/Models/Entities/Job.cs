using HireHub.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HireHub.Models.Entities
{
    public class Job
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Job title is required.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Title must be 3-150 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(5000, MinimumLength = 10, ErrorMessage = "Description must be at least 10 characters.")]
        public string Description { get; set; } = string.Empty;

        [StringLength(3000)]
        public string? Requirements { get; set; }

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(200)]
        public string? Location { get; set; }

        [Range(0, 999999999, ErrorMessage = "Salary must be a positive number.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? SalaryMin { get; set; }

        [Range(0, 999999999, ErrorMessage = "Salary must be a positive number.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? SalaryMax { get; set; }

        [Required(ErrorMessage = "Job type is required.")]
        public JobType JobType { get; set; }

        [Required(ErrorMessage = "Experience level is required.")]
        public ExperienceLevel ExperienceLevel { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime PostedAt { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Deadline { get; set; }

        // Foreign Keys
        public int CompanyId { get; set; }
        public string PostedByUserId { get; set; } = string.Empty;

        // Navigation
        public Company Company { get; set; } = null!;
        public ApplicationUser PostedBy { get; set; } = null!;
        public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
        public ICollection<SavedJob> SavedByUsers { get; set; } = new List<SavedJob>();
    }
}