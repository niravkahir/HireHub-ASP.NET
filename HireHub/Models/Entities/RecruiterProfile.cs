using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace HireHub.Models.Entities
{
    public class RecruiterProfile
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;

        public string? Designation { get; set; }
        public string? Phone { get; set; }
        public int? CompanyId { get; set; }
        public ApplicationUser User { get; set; } = null!;
        public Company? Company { get; set; }
    }
}