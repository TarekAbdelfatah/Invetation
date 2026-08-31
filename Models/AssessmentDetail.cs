namespace Ibtikar.Models
{
    public class AssessmentDetail
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AssessmentHeaderId { get; set; }
        public Guid CriterionId { get; set; }
        public int Score { get; set; }
        public string? Comment { get; set; }

        public AssessmentHeader? AssessmentHeader { get; set; }
        public AssessmentCriterion? Criterion { get; set; }
    }
}