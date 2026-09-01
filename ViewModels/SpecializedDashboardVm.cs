using System.ComponentModel.DataAnnotations;

namespace Ibtikar.ViewModels
{
    public class SpecializedDashboardVm
    {
        public int UnderStudy { get; set; }
        public int SentToPartner { get; set; }
        public int SentToExecution { get; set; }
        public int RejectedAfterRouting { get; set; }
        public string? DepartmentName { get; set; }

        public int AdvisoryPending { get; set; }
        public int AdvisoryLate { get; set; }
        public int AdvisorySubmitted { get; set; }
        public List<PartnerAssignmentRowVm> AdvisoryItems { get; set; } = new();
    }

    public class SpecializedReferralsVm
    {
        public List<SpecializedReferralRowVm> Items { get; set; } = new();
        public string StatusFilter { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
    }

    public sealed record SpecializedReferralRowVm(
        Guid Id,
        string Reference,
        string Title,
        string StatusCode,
        string StatusName,
        string StatusColor,
        DateTime? AssignedAt,
        double StayDays,
        string? ApplicantName,
        bool IsOverdue);

    public class SpecializedDetailsVm
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ProblemStatement { get; set; }
        public string? ProposedSolution { get; set; }
        public string? ExpectedBenefits { get; set; }
        public string? DomainName { get; set; }
        public string? ExpectedImpactName { get; set; }
        public string? TargetAudienceName { get; set; }
        public string? ApplicantName { get; set; }
        public string? ApplicantDepartmentName { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string StatusColor { get; set; } = "#6c757d";
        public string StatusCode { get; set; } = string.Empty;
        public DateTime? SubmittedAt { get; set; }
        public DateTime? AssignedAt { get; set; }
        public bool CanReturnNotCompetent { get; set; }
        public List<SpecializedAttachmentVm> Attachments { get; set; } = new();
        public List<SpecializedHistoryRowVm> History { get; set; } = new();
    }

    public sealed record SpecializedAttachmentVm(Guid Id, string FileName, long SizeBytes, DateTime UploadedAt);
    public sealed record SpecializedHistoryRowVm(DateTime ChangedAt, string FromStatus, string ToStatus, string By, string? Note);

    public class SpecializedAssessVm
    {
        public Guid IdeaId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public string StatusColor { get; set; } = "#6c757d";
        public bool IsDraftSaved { get; set; }
        public bool IsLocked { get; set; }
        public Guid? DraftHeaderId { get; set; }
        public DateTime? DraftSavedAt { get; set; }
        public List<SpecializedCriterionVm> Criteria { get; set; } = new();
        public List<SpecializedAssessmentLineVm> Lines { get; set; } = new();
        public decimal? TotalScore { get; set; }
        public string? Comment { get; set; }
    }

    public sealed record SpecializedCriterionVm(Guid Id, string Code, string Name, string? Description, int DisplayOrder);
    public sealed record SpecializedAssessmentLineVm(Guid CriterionId, string CriterionCode, string CriterionName, int? Score, string? Comment);

    public class SpecializedRequestVm
    {
        public Guid IdeaId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<SpecializedPartnerOptionVm> AvailablePartners { get; set; } = new();
        public List<SpecializedPartnerOptionVm> AlreadyAssigned { get; set; } = new();
        public List<Guid> SelectedPartnerIds { get; set; } = new();
        public List<Guid> PartnerIds { get; set; } = new();
    }

    public sealed class SpecializedRequestSubmitVm
    {
        public const int MaxPartners = 2;

        public Guid IdeaId { get; set; }

        [Required(ErrorMessage = "يرجى اختيار جهة واحدة على الأقل لطلب الرأي.")]
        [MinLength(1, ErrorMessage = "يرجى اختيار جهة واحدة على الأقل لطلب الرأي.")]
        [MaxLength(MaxPartners, ErrorMessage = "لا يمكن طلب رأي أكثر من إدارتين في المرة الواحدة.")]
        public List<Guid> PartnerIds { get; set; } = new();

        public List<SpecializedRequestPartnerNoteVm> Notes { get; set; } = new();
    }

    public sealed class SpecializedRequestPartnerNoteVm
    {
        public Guid PartnerId { get; set; }

        [MaxLength(2000, ErrorMessage = "الملاحظة يجب ألا تتجاوز 2000 حرف.")]
        public string? Note { get; set; }
    }

    public sealed record SpecializedPartnerOptionVm(Guid Id, string Name, string? Code);

    public class SpecializedPartnerOpinionVm
    {
        public Guid IdeaId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<SpecializedPartnerFollowUpVm> Rows { get; set; } = new();
    }

    public sealed record SpecializedPartnerScoreLineVm(
        Guid CriterionId,
        string CriterionCode,
        string CriterionName,
        int Score,
        string? Comment);

    public sealed record SpecializedPartnerFollowUpVm(
        Guid AssignmentId,
        Guid IdeaId,
        string IdeaReference,
        string IdeaTitle,
        string PartnerDepartmentName,
        string Status,
        string StatusBadgeClass,
        DateTime SentAt,
        DateTime? RespondedAt,
        double DaysOpen,
        bool IsLate,
        string? Note,
        bool HasResponse,
        string? ResponseComment,
        decimal? TotalScore,
        DateTime? ResponseSubmittedAt,
        IReadOnlyList<SpecializedPartnerScoreLineVm> Scores);

    public class SpecializedSendToCommitteeVm
    {
        public Guid IdeaId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public int TotalCriteria { get; set; }
        public int CompletedCriteria { get; set; }
        public int UnrepliedPartners { get; set; }
        public bool CanSend { get; set; }
        public string? WarningMessage { get; set; }
    }
}