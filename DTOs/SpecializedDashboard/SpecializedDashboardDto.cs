namespace Ibtikar.DTOs.SpecializedDashboard
{
    public sealed record SpecializedDashboardDto(
        int UnderStudy,
        int SentToPartner,
        int SentToExecution,
        int RejectedAfterRouting);

    public sealed record SpecializedDetailsDto(
        Guid Id,
        string Reference,
        string Title,
        string Description,
        string? ProblemStatement,
        string? ProposedSolution,
        string? ExpectedBenefits,
        string? DomainName,
        string? ExpectedImpactName,
        string? TargetAudienceName,
        string? ApplicantName,
        string? ApplicantDepartmentName,
        string StatusName,
        string StatusColor,
        string StatusCode,
        DateTime? SubmittedAt,
        DateTime? AssignedAt,
        bool CanReturnNotCompetent,
        IReadOnlyList<SpecializedAttachmentDto> Attachments,
        IReadOnlyList<SpecializedHistoryRowDto> History);

    public sealed record SpecializedAttachmentDto(
        Guid Id,
        string FileName,
        long SizeBytes,
        DateTime UploadedAt);

    public sealed record SpecializedHistoryRowDto(
        DateTime ChangedAt,
        string FromStatus,
        string ToStatus,
        string By,
        string? Note);

    public sealed record SpecializedReferralRowDto(
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

    public sealed record SpecializedReferralsDto(
        IReadOnlyList<SpecializedReferralRowDto> Items,
        string StatusFilter,
        int Page,
        int PageSize,
        int TotalCount);

    public sealed record SpecializedStatusOptionDto(
        Guid Id,
        string Code,
        string Name,
        string Color);

    public sealed record SpecializedCriterionDto(
        Guid Id,
        string Code,
        string Name,
        string? Description,
        int DisplayOrder);

    public sealed record SpecializedAssessmentLineDto(
        Guid CriterionId,
        string CriterionCode,
        string CriterionName,
        int? Score,
        string? Comment);

    public sealed record SpecializedAssessVmDto(
        Guid IdeaId,
        string Reference,
        string Title,
        string StatusName,
        string StatusColor,
        bool IsDraftSaved,
        bool IsLocked,
        Guid? DraftHeaderId,
        DateTime? DraftSavedAt,
        IReadOnlyList<SpecializedCriterionDto> Criteria,
        IReadOnlyList<SpecializedAssessmentLineDto> Lines,
        decimal? TotalScore,
        string? Comment);

    public sealed record SpecializedAssessmentSubmissionDto(
        Guid IdeaId,
        Guid? HeaderId,
        IReadOnlyList<SpecializedScoreInputDto> Scores,
        string? Comment,
        bool IsDraft);

    public sealed record SpecializedScoreInputDto(Guid CriterionId, int Score, string? Comment);

    public sealed record SpecializedAssessmentOutcomeDto(
        bool Success,
        bool SavedAsDraft,
        string? Message,
        Guid? HeaderId,
        decimal? TotalScore);

    public sealed record SpecializedPartnerOptionDto(Guid Id, string Name, string? Code);

    public sealed record SpecializedRequestDto(
        Guid IdeaId,
        string Reference,
        string Title,
        IReadOnlyList<SpecializedPartnerOptionDto> AvailablePartners,
        IReadOnlyList<SpecializedPartnerOptionDto> AlreadyAssigned);

    public sealed record SpecializedRequestSubmissionDto(
        Guid IdeaId,
        IReadOnlyList<Guid> PartnerDepartmentIds,
        IReadOnlyList<SpecializedRequestPartnerNoteDto> PartnerNotes);

    public sealed record SpecializedRequestPartnerNoteDto(Guid PartnerDepartmentId, string? Note);

    public sealed record SpecializedRequestOutcomeDto(
        bool Success,
        string? Message,
        int Created);

    public sealed record SpecializedPartnerScoreLineDto(
        Guid CriterionId,
        string CriterionCode,
        string CriterionName,
        int Score,
        string? Comment);

    public sealed record SpecializedPartnerFollowUpRowDto(
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
        IReadOnlyList<SpecializedPartnerScoreLineDto> Scores);

    public sealed record SpecializedPartnerOpinionDto(
        Guid IdeaId,
        string Reference,
        string Title,
        IReadOnlyList<SpecializedPartnerFollowUpRowDto> Rows);

    public sealed record SpecializedSendToCommitteeDto(
        Guid IdeaId,
        string Reference,
        int TotalCriteria,
        int CompletedCriteria,
        int UnrepliedPartners,
        bool CanSend,
        string? WarningMessage);

    public sealed record SpecializedSendToCommitteeOutcomeDto(
        bool Success,
        string? Message,
        bool RequiresConfirmation);

    public sealed record SpecializedReturnNotCompetentOutcomeDto(
        bool Success,
        string? Message);
}