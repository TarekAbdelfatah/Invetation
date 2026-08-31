namespace Ibtikar.Models
{
    public class AuditActionItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid IdeaId { get; set; }
        public string Decision { get; set; } = string.Empty;
        public string? DecisionText { get; set; }
        public Guid? TargetDepartmentId { get; set; }
        public Guid AuditorId { get; set; }
        public DateTime AuditDate { get; set; } = DateTime.UtcNow;

        public InnovationIdea? Idea { get; set; }
        public Department? TargetDepartment { get; set; }
        public User? Auditor { get; set; }
    }
}
