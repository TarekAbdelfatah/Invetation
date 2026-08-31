namespace Ibtikar.Models
{
    public class InnovationCommittee
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ActivatedAt { get; set; }
        public Guid CreatedByUserId { get; set; }

        public User? CreatedBy { get; set; }
        public List<CommitteeMember> Members { get; set; } = new();
    }
}
