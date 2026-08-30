namespace Ibtikar.Models
{
    public class IdeaStatus
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string Color { get; set; } = "#6c757d";
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsTerminal { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}