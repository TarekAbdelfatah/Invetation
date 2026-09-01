using Ibtikar.DTOs.Reports;

namespace Ibtikar.ViewModels
{
    public class ReportsVm
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public bool ShowValidation { get; set; }
        public string? ValidationMessage { get; set; }
        public ReportsKpiVm Kpis { get; set; } = new();
        public List<ReportsStageMixRowVm> StageMix { get; set; } = new();
        public string? Warning { get; set; }
        public bool IsEmpty => Kpis.TotalIdeas == 0;
    }

    public class ReportsKpiVm
    {
        public int TotalIdeas { get; set; }
        public int Submitted { get; set; }
        public int Approved { get; set; }
        public int InExecution { get; set; }
        public int Completed { get; set; }
    }

    public sealed record ReportsStageMixRowVm(
        string Code,
        string Name,
        string Color,
        int Count,
        double Percent);

    public class ReportsChallengesVm
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public Guid? DomainId { get; set; }
        public List<ReportsDomainOptionVm> Domains { get; set; } = new();
        public List<ReportsChallengeRowVm> Rows { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public sealed record ReportsDomainOptionVm(Guid Id, string Name);

    public sealed record ReportsChallengeRowVm(
        Guid IdeaId,
        string Reference,
        string Title,
        string DomainName,
        string ApplicantName,
        string ApplicantDepartmentName,
        string ProblemStatement,
        string? ProposedSolution,
        DateTime CreatedAt,
        string StatusName,
        string StatusColor);
}
