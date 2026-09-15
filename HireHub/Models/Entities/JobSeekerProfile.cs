namespace HireHub.Models.Entities
{
    public class JobSeekerProfile
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Location { get; set; }
        public string? Skills { get; set; }        
        public string? Education { get; set; }
        public string? Experience { get; set; }
        public string? ResumePath { get; set; }     // resume path

        public ApplicationUser User { get; set; } = null!;
        public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
        public ICollection<SavedJob> SavedJobs { get; set; } = new List<SavedJob>();
    }
}