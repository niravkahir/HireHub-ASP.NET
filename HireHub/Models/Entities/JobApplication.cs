using HireHub.Models.Enums;

namespace HireHub.Models.Entities
{
    public class JobApplication
    {
        public int Id { get; set; }

        public int JobId { get; set; }
        public int JobSeekerProfileId { get; set; }

        public string? CoverLetter { get; set; }
        public string? ResumePath { get; set; }

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public Job Job { get; set; } = null!;
        public JobSeekerProfile JobSeekerProfile { get; set; } = null!;
        public Interview? Interview { get; set; }
    }
}