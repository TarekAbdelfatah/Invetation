namespace Ibtikar.ViewModels
{
    public class AuditInboxVm
    {
        public List<Row> Items { get; }
        public string ApplicantType { get; }
        public string Status { get; }
        public AuditInboxVm(List<Row> items, string applicantType, string status)
        {
            Items = items;
            ApplicantType = applicantType ?? string.Empty;
            Status = status ?? string.Empty;
        }
        public record Row(Guid Id, string Reference, string Title, string Domain, string ApplicantName, string Department, DateTime SubmittedAt, bool IsOverdue);
    }

    public class AuditDetailsVm
    {
        public Guid Id { get; }
        public string Reference { get; }
        public string Title { get; }
        public string Description { get; }
        public string? ProblemStatement { get; }
        public string? ProposedSolution { get; }
        public string? ExpectedBenefits { get; }
        public string Domain { get; }
        public string ApplicantName { get; }
        public string ApplicantDepartment { get; }
        public string? AssignedDepartment { get; }
        public string StatusName { get; }
        public string StatusColor { get; }
        public DateTime SubmittedAt { get; }
        public bool CanOpen { get; }
        public bool IsUnderStudy { get; }
        public bool IsRoutedToSpecialist { get; }
        public bool IsTerminal { get; }
        public List<DepartmentOption> ActiveDepartments { get; }
        public List<AuditHistoryRow> History { get; }

        public AuditDetailsVm(
            Guid id,
            string reference,
            string title,
            string description,
            string? problemStatement,
            string? proposedSolution,
            string? expectedBenefits,
            string domain,
            string applicantName,
            string applicantDepartment,
            string? assignedDepartment,
            string statusName,
            string statusColor,
            DateTime submittedAt,
            bool canOpen,
            bool isUnderStudy,
            bool isRoutedToSpecialist,
            bool isTerminal,
            List<DepartmentOption> activeDepartments,
            List<AuditHistoryRow> history)
        {
            Id = id;
            Reference = reference;
            Title = title;
            Description = description;
            ProblemStatement = problemStatement;
            ProposedSolution = proposedSolution;
            ExpectedBenefits = expectedBenefits;
            Domain = domain;
            ApplicantName = applicantName;
            ApplicantDepartment = applicantDepartment;
            AssignedDepartment = assignedDepartment;
            StatusName = statusName;
            StatusColor = statusColor;
            SubmittedAt = submittedAt;
            CanOpen = canOpen;
            IsUnderStudy = isUnderStudy;
            IsRoutedToSpecialist = isRoutedToSpecialist;
            IsTerminal = isTerminal;
            ActiveDepartments = activeDepartments;
            History = history;
        }

        public record DepartmentOption(Guid Id, string Name);
        public record AuditHistoryRow(DateTime ChangedAt, string FromStatus, string ToStatus, string By, string? Note);
    }
}