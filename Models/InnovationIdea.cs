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

        public Guid InnovationDomainId { get; set; }
        public Guid? ExpectedImpactId { get; set; }
        public Guid? TargetAudienceId { get; set; }
        public Guid CurrentStatusId { get; set; }
        public Guid ApplicantUserId { get; set; }
        public Guid? ApplicantDepartmentId { get; set; }

        public bool IsDraft { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }

        public InnovationDomain? InnovationDomain { get; set; }
        public ExpectedImpact? ExpectedImpact { get; set; }
        public TargetAudience? TargetAudience { get; set; }
        public IdeaStatus? CurrentStatus { get; set; }
        public User? ApplicantUser { get; set; }
        public Department? ApplicantDepartment { get; set; }
    }
}
