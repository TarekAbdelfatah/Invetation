namespace Ibtikar.Models
{
    public class IdeaStatusHistory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid InnovationIdeaId { get; set; }
        public Guid? FromStatusId { get; set; }
        public Guid ToStatusId { get; set; }
        public Guid? ChangedByUserId { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public string? Note { get; set; }

        public InnovationIdea? InnovationIdea { get; set; }
        public IdeaStatus? FromStatus { get; set; }
        public IdeaStatus? ToStatus { get; set; }
        public User? ChangedBy { get; set; }
    }
}
