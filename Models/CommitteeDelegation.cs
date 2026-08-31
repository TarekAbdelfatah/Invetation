namespace Ibtikar.Models
{
    public class CommitteeDelegation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid InnovationCommitteeId { get; set; }
        public Guid HeadUserId { get; set; }
        public Guid DelegateMemberUserId { get; set; }
        public DateTime StartAt { get; set; } = DateTime.UtcNow;
        public DateTime EndAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public InnovationCommittee? InnovationCommittee { get; set; }
        public User? Head { get; set; }
        public User? DelegateMember { get; set; }
    }
}
