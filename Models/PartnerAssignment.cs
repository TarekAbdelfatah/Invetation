namespace Ibtikar.Models
{
    public class PartnerAssignment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid InnovationIdeaId { get; set; }
        public Guid PartnerDepartmentId { get; set; }
        public Guid RequestedByUserId { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }

        public const string StatusPending = "Pending";
        public const string StatusSubmitted = "Submitted";
        public const string StatusReturned = "Returned";
        public const string StatusLate = "Late";

        public string Status { get; set; } = StatusPending;
        public string? Note { get; set; }

        public InnovationIdea? InnovationIdea { get; set; }
        public Department? PartnerDepartment { get; set; }
        public User? RequestedBy { get; set; }
    }
}