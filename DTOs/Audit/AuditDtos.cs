namespace Ibtikar.DTOs.Audit
{
    public sealed record AuditInboxRowDto(
        Guid Id,
        string Reference,
        string Title,
        string Domain,
        string ApplicantName,
        string Department,
        string? AssignedDepartment,
        string StatusCode,
        string StatusName,
        string StatusColor,
        DateTime SubmittedAt,
        bool IsOverdue,
        bool IsReturnedBySpecialist,
        string? ReturnedReason,
        DateTime? ReturnedAt);

    public sealed record AuditInboxDto(
        IReadOnlyList<AuditInboxRowDto> Items,
        string ApplicantType,
        string Status,
        int Page,
        int PageSize,
        int TotalCount);

    public sealed record AuditDepartmentOptionDto(Guid Id, string Name);

    public sealed record AuditAttachmentDto(
        Guid Id,
        string FileName,
        long SizeBytes,
        string ContentType,
        DateTime UploadedAt);

    public sealed record AuditHistoryRowDto(
        DateTime ChangedAt,
        string FromStatus,
        string ToStatus,
        string By,
        string? Note);

    public sealed record AuditDetailsDto(
        Guid Id,
        string Reference,
        string Title,
        string Description,
        string? ProblemStatement,
        string? ProposedSolution,
        string? ExpectedBenefits,
        string? RequiredResources,
        string? ExpectedImpactName,
        string? ExpectedImpactOther,
        string? TargetAudienceName,
        string? TargetAudienceOther,
        bool UsesEmergingTech,
        string? TechnologyOther,
        string Domain,
        string ApplicantName,
        string ApplicantDepartment,
        string? AssignedDepartment,
        string StatusCode,
        string StatusName,
        string StatusColor,
        DateTime SubmittedAt,
        bool CanDecide,
        bool IsUnderStudy,
        bool IsRoutedToSpecialist,
        bool IsTerminal,
        string? LatestCompletionNote,
        DateTime? LatestCompletionNoteAt,
        string? ReturnedBySpecialistReason,
        string? ReturnedBySpecialistDepartment,
        DateTime? ReturnedBySpecialistAt,
        IReadOnlyList<AuditDepartmentOptionDto> ActiveDepartments,
        IReadOnlyList<AuditHistoryRowDto> History,
        IReadOnlyList<AuditAttachmentDto> Attachments);

    public enum AuditActionOutcome
    {
        Success,
        NotFound,
        InvalidState,
        InvalidInput
    }

    public sealed record AuditActionResultDto(AuditActionOutcome Outcome, string? Message);
}