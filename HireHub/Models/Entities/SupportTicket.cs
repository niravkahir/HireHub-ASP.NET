using HireHub.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace HireHub.Models.Entities
{
    public class SupportTicket
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(150, MinimumLength = 5, ErrorMessage = "Subject must be 5-150 characters.")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required.")]
        [StringLength(3000, MinimumLength = 10, ErrorMessage = "Message must be at least 10 characters.")]
        public string Message { get; set; } = string.Empty;

        public SupportStatus Status { get; set; } = SupportStatus.Open;

        public string? AdminReply { get; set; }
        public DateTime? RepliedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ApplicationUser User { get; set; } = null!;
    }
}