namespace Ibtikar.Models
{
    public class InnovationIdea
    {
        public Guid Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ProblemStatement { get; set; }
        public string? ProposedSolution { get; set; }
        public string? ExpectedBenefits { get; set; }
        public string? ExpectedImpactOther { get; set; }
        public string? TargetAudienceOther { get; set; }
        public bool UsesEmergingTech { get; set; }
        public string? TechnologyOther { get; set; }
        public string? RequiredResources { get; set; }

        public Guid InnovationDomainId { get; set; }
        public Guid? ExpectedImpactId { get; set; }
        public Guid? TargetAudienceId { get; set; }
        public Guid CurrentStatusId { get; set; }
        public Guid ApplicantUserId { get; set; }
        public Guid? ApplicantDepartmentId { get; set; }
        public Guid? AssignedDepartmentId { get; set; }
        public Guid? AuditEmployeeId { get; set; }
        public DateTime? AuditAssignedAt { get; set; }

        public bool IsDraft { get; set; } = true;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }

        public InnovationDomain? InnovationDomain { get; set; }
        public ExpectedImpact? ExpectedImpact { get; set; }
        public TargetAudience? TargetAudience { get; set; }
        public IdeaStatus? CurrentStatus { get; set; }
        public User? ApplicantUser { get; set; }
        public Department? ApplicantDepartment { get; set; }
        public Department? AssignedDepartment { get; set; }
        public List<IdeaAttachment> Attachments { get; set; } = new();
        public List<IdeaStatusHistory> StatusHistory { get; set; } = new();
        public List<AuditActionItem> AuditActions { get; set; } = new();
    }
}
