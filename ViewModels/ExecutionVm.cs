using System.ComponentModel.DataAnnotations;
using Ibtikar.DTOs.Execution;

namespace Ibtikar.ViewModels
{
    public sealed class ExecutionListVm
    {
        public string DepartmentName { get; set; } = string.Empty;
        public List<ExecutionListRowVm> Items { get; set; } = new();
    }

    public sealed record ExecutionListRowVm(
        Guid IdeaId,
        string Reference,
        string Title,
        string DomainName,
        string ApplicantName,
        DateTime? AssignedAt,
        string? CurrentStageName,
        string StatusName,
        string StatusColor,
        bool CanUpdate,
        bool CanComplete);

    public sealed class ExecutionHeaderVm
    {
        public Guid IdeaId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string DomainName { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantDepartmentName { get; set; } = string.Empty;
        public string AssignedDepartmentName { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public string StatusColor { get; set; } = "#6c757d";
        public List<ExecutionStageOptionVm> Stages { get; set; } = new();
        public Guid? CurrentStageId { get; set; }
        public string? CurrentStageName { get; set; }
        public int CurrentStageOrder { get; set; }
        public bool CanUpdate { get; set; }
        public bool CanComplete { get; set; }
        public string? Note { get; set; }
        public Guid? ExecutionStageId { get; set; }
        public string? CompleteNote { get; set; }
        public Guid? CompletionStageId { get; set; }
    }

    public sealed record ExecutionStageOptionVm(
        Guid Id,
        int Order,
        string Code,
        string Name);

    public sealed class ExecutionTimelineVm
    {
        public Guid IdeaId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<ExecutionTimelineRowVm> Rows { get; set; } = new();
    }

    public sealed record ExecutionTimelineRowVm(
        DateTime ChangedAt,
        string StageName,
        int StageOrder,
        string? ChangedByName,
        string? Note);

    public sealed class ExecutionStageUpdateVm
    {
        public Guid ExecutionStageId { get; set; }

        [Required(ErrorMessage = "ملاحظة التحديث مطلوبة.")]
        [MinLength(5, ErrorMessage = "ملاحظة التحديث يجب ألا تقل عن 5 أحرف.")]
        public string? Note { get; set; }
    }

    public sealed class ExecutionCompleteVm
    {
        public Guid CompletionStageId { get; set; }

        [Required(ErrorMessage = "ملاحظة الإكمال مطلوبة.")]
        [MinLength(5, ErrorMessage = "ملاحظة الإكمال يجب ألا تقل عن 5 أحرف.")]
        public string? CompleteNote { get; set; }

        public Guid? AttachmentIdeaId { get; set; }
    }
}
