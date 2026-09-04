using System.ComponentModel.DataAnnotations;
using Ibtikar.Validation;

namespace Ibtikar.ViewModels
{
    public sealed class CommitteeSummaryVm
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public string HeadUserName { get; set; } = "—";
        public int MemberCount { get; set; }
    }

    public sealed class CommitteeMemberOptionVm
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }

    public sealed class CommitteesIndexVm
    {
        public List<CommitteeSummaryVm> Committees { get; set; } = new();
    }

    public sealed class CommitteesCreateVm
    {
        [Required(ErrorMessage = "اسم اللجنة مطلوب.")]
        [StringLength(150, ErrorMessage = "اسم اللجنة يجب ألا يتجاوز 150 حرفاً.")]
        [NoHtml]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "الوصف يجب ألا يتجاوز 1000 حرف.")]
        [NoHtml]
        public string? Description { get; set; }

        [Required(ErrorMessage = "يرجى اختيار رئيس اللجنة.")]
        public Guid? HeadUserId { get; set; }

        [MinLength(1, ErrorMessage = "يرجى إضافة عضو واحد على الأقل.")]
        public List<Guid> MemberUserIds { get; set; } = new();
        public List<CommitteeMemberOptionVm> MemberCandidates { get; set; } = new();
        public List<CommitteeMemberOptionVm> HeadCandidates { get; set; } = new();
    }

    public sealed class CommitteeDetailVm
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public string HeadUserName { get; set; } = string.Empty;
        public string HeadUsername { get; set; } = string.Empty;
        public List<CommitteeMemberDetailVm> Members { get; set; } = new();
    }

    public sealed class CommitteeMemberDetailVm
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public bool IsHead { get; set; }
    }

    public sealed class CommitteesEditVm
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "اسم اللجنة مطلوب.")]
        [StringLength(150, ErrorMessage = "اسم اللجنة يجب ألا يتجاوز 150 حرفاً.")]
        [NoHtml]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "الوصف يجب ألا يتجاوز 1000 حرف.")]
        [NoHtml]
        public string? Description { get; set; }

        [Required(ErrorMessage = "يرجى اختيار رئيس اللجنة.")]
        public Guid? HeadUserId { get; set; }

        [MinLength(1, ErrorMessage = "يرجى إضافة عضو واحد على الأقل.")]
        public List<Guid> MemberUserIds { get; set; } = new();
        public List<CommitteeMemberOptionVm> MemberCandidates { get; set; } = new();
        public List<CommitteeMemberOptionVm> HeadCandidates { get; set; } = new();
    }
}
