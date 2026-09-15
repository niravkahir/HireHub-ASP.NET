namespace HireHub.Models.Entities
{
    public class SavedJob
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public int JobSeekerProfileId { get; set; }
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;

        public Job Job { get; set; } = null!;
        public JobSeekerProfile JobSeekerProfile { get; set; } = null!;
    }
}