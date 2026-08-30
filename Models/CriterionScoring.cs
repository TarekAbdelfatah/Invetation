namespace Ibtikar.Models
{
    public class CriterionScoring
    {
        public Guid Id { get; set; }
        public int Score { get; set; }
        public int Percent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}