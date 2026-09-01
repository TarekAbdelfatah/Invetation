namespace Ibtikar.DTOs.Audit
{
    public sealed record AuditInboxRowDto(
        Guid Id,
        string Reference,
        string Title,
        string Domain,
        string ApplicantName,
        string Department,
        DateTime SubmittedAt,
        bool IsOverdue);

    public sealed record AuditInboxDto(
        IReadOnlyList<AuditInboxRowDto> Items,
        string ApplicantType,
        string Status);

    public sealed record AuditDepartmentOptionDto(Guid Id, string Name);

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
        string Domain,
        string ApplicantName,
        string ApplicantDepartment,
        string? AssignedDepartment,
        string StatusName,
        string StatusColor,
        DateTime SubmittedAt,
        bool CanOpen,
        bool IsUnderStudy,
        bool IsRoutedToSpecialist,
        bool IsTerminal,
        IReadOnlyList<AuditDepartmentOptionDto> ActiveDepartments,
        IReadOnlyList<AuditHistoryRowDto> History);

    public enum AuditActionOutcome
    {
        Success,
        NotFound,
        InvalidState,
        InvalidInput
    }

    public sealed record AuditActionResultDto(AuditActionOutcome Outcome, string? Message);
}