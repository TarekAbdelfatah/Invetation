namespace Ibtikar.Models
{
    public class AssessmentHeader
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid InnovationIdeaId { get; set; }
        public Guid AssessorUserId { get; set; }
        public Guid AssessorDepartmentId { get; set; }

        public const string SourceSpecialized = "specialized";
        public const string SourcePartner = "partner";
        public const string SourceCommittee = "committee";

        public string Source { get; set; } = SourceSpecialized;
        public bool IsDraft { get; set; } = true;
        public bool IsLocked { get; set; }
        public DateTime? LockedAt { get; set; }

        public decimal? TotalScore { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }

        public InnovationIdea? InnovationIdea { get; set; }
        public User? Assessor { get; set; }
        public Department? AssessorDepartment { get; set; }
        public List<AssessmentDetail> Details { get; set; } = new();
    }
}