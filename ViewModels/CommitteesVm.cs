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
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? HeadUserId { get; set; }
        public List<Guid> MemberUserIds { get; set; } = new();
        public List<CommitteeMemberOptionVm> MemberCandidates { get; set; } = new();
        public List<CommitteeMemberOptionVm> HeadCandidates { get; set; } = new();
    }
}
