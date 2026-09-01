using System.ComponentModel.DataAnnotations;

namespace Ibtikar.Models
{
    public class ExecutionProgress
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid InnovationIdeaId { get; set; }

        public Guid ExecutionStageId { get; set; }

        [Required]
        [StringLength(500)]
        public string Note { get; set; } = string.Empty;

        public Guid? ChangedByUserId { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public InnovationIdea? InnovationIdea { get; set; }

        public ExecutionStage? ExecutionStage { get; set; }

        public User? ChangedBy { get; set; }
    }
}
