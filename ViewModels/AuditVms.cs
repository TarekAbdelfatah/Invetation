namespace Ibtikar.ViewModels
{
    public class AuditInboxVm
    {
        public List<Row> Items { get; }
        public string ApplicantType { get; }
        public string Status { get; }
        public int Page { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
        public AuditInboxVm(List<Row> items, string applicantType, string status, int page, int pageSize, int totalCount)
        {
            Items = items;
            ApplicantType = applicantType ?? string.Empty;
            Status = status ?? string.Empty;
            Page = page;
            PageSize = pageSize;
            TotalCount = totalCount;
        }
        public record Row(
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
            bool IsOverdue);
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
        public string? RequiredResources { get; }
        public string? ExpectedImpactName { get; }
        public string? ExpectedImpactOther { get; }
        public string? TargetAudienceName { get; }
        public string? TargetAudienceOther { get; }
        public bool UsesEmergingTech { get; }
        public string? TechnologyOther { get; }
        public string Domain { get; }
        public string ApplicantName { get; }
        public string ApplicantDepartment { get; }
        public string? AssignedDepartment { get; }
        public string StatusCode { get; }
        public string StatusName { get; }
        public string StatusColor { get; }
        public DateTime SubmittedAt { get; }
        public bool CanDecide { get; }
        public bool IsUnderStudy { get; }
        public bool IsRoutedToSpecialist { get; }
        public bool IsTerminal { get; }
        public bool IsRouted { get; }
        public List<DepartmentOption> ActiveDepartments { get; }
        public List<AuditHistoryRow> History { get; }
        public List<Attachment> Attachments { get; }

        public AuditDetailsVm(
            Guid id,
            string reference,
            string title,
            string description,
            string? problemStatement,
            string? proposedSolution,
            string? expectedBenefits,
            string? requiredResources,
            string? expectedImpactName,
            string? expectedImpactOther,
            string? targetAudienceName,
            string? targetAudienceOther,
            bool usesEmergingTech,
            string? technologyOther,
            string domain,
            string applicantName,
            string applicantDepartment,
            string? assignedDepartment,
            string statusCode,
            string statusName,
            string statusColor,
            DateTime submittedAt,
            bool canDecide,
            bool isUnderStudy,
            bool isRoutedToSpecialist,
            bool isTerminal,
            List<DepartmentOption> activeDepartments,
            List<AuditHistoryRow> history,
            List<Attachment> attachments)
        {
            Id = id;
            Reference = reference;
            Title = title;
            Description = description;
            ProblemStatement = problemStatement;
            ProposedSolution = proposedSolution;
            ExpectedBenefits = expectedBenefits;
            RequiredResources = requiredResources;
            ExpectedImpactName = expectedImpactName;
            ExpectedImpactOther = expectedImpactOther;
            TargetAudienceName = targetAudienceName;
            TargetAudienceOther = targetAudienceOther;
            UsesEmergingTech = usesEmergingTech;
            TechnologyOther = technologyOther;
            Domain = domain;
            ApplicantName = applicantName;
            ApplicantDepartment = applicantDepartment;
            AssignedDepartment = assignedDepartment;
            StatusCode = statusCode;
            StatusName = statusName;
            StatusColor = statusColor;
            SubmittedAt = submittedAt;
            CanDecide = canDecide;
            IsUnderStudy = isUnderStudy;
            IsRoutedToSpecialist = isRoutedToSpecialist;
            IsTerminal = isTerminal;
            IsRouted = isRoutedToSpecialist && !string.IsNullOrWhiteSpace(assignedDepartment);
            ActiveDepartments = activeDepartments;
            History = history;
            Attachments = attachments;
        }

        public record DepartmentOption(Guid Id, string Name);
        public record AuditHistoryRow(DateTime ChangedAt, string FromStatus, string ToStatus, string By, string? Note);
        public record Attachment(Guid Id, string FileName, long SizeBytes, string ContentType, DateTime UploadedAt);
    }
}