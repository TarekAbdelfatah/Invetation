namespace Ibtikar.Models
{
    public class CommitteeVote
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid InnovationIdeaId { get; set; }
        public Guid MemberUserId { get; set; }

        public const string DecisionAgree = "Agree";
        public const string DecisionDisagree = "Disagree";
        public const string DecisionNeedsDevelopment = "NeedsDevelopment";

        public string Decision { get; set; } = DecisionAgree;
        public string? Note { get; set; }
        public DateTime VotedAt { get; set; } = DateTime.UtcNow;

        public InnovationIdea? InnovationIdea { get; set; }
        public User? Member { get; set; }
    }
}
