namespace HireHub.Models.Entities
{
    public class Interview
    {
        public int Id { get; set; }

        public int JobApplicationId { get; set; }
        public DateTime ScheduledAt { get; set; }
        public string? Mode { get; set; }          
        public string? Location { get; set; }      
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public JobApplication JobApplication { get; set; } = null!;
    }
}