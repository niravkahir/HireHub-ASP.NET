using HireHub.Models.Entities;
using HireHub.Models.Enums;

namespace HireHub.Models.ViewModels
{
    public class JobSearchViewModel
    {
        public string? Keyword { get; set; }
        public string? Location { get; set; }
        public JobType? JobType { get; set; }
        public ExperienceLevel? ExperienceLevel { get; set; }
        public decimal? MinSalary { get; set; }

        public List<Job> Results { get; set; } = new();
    }
}