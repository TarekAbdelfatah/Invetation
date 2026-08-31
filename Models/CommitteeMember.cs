namespace Ibtikar.Models
{
    public class CommitteeMember
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid InnovationCommitteeId { get; set; }
        public Guid UserId { get; set; }
        public bool IsHead { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public InnovationCommittee? InnovationCommittee { get; set; }
        public User? User { get; set; }
    }
}
