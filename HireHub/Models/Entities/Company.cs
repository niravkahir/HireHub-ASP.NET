namespace HireHub.Models.Entities
{
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Website { get; set; }
        public string? Location { get; set; }
        public string? LogoPath { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<Job> Jobs { get; set; } = new List<Job>();
        public ICollection<RecruiterProfile> Recruiters { get; set; } = new List<RecruiterProfile>();
    }
}