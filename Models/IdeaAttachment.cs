namespace Ibtikar.Models
{
    public class IdeaAttachment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid InnovationIdeaId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/pdf";
        public long SizeBytes { get; set; }
        public string StoragePath { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public Guid UploadedByUserId { get; set; }

        public InnovationIdea? InnovationIdea { get; set; }
        public User? UploadedBy { get; set; }
    }
}
