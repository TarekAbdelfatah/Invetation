namespace Ibtikar.ViewModels
{
    public class AdminOverviewVm
    {
        public int TotalIdeas { get; set; }
        public int Drafts { get; set; }
        public int Submitted { get; set; }
        public int TotalUsers { get; set; }
        public List<StatusCount> ByStatus { get; set; } = new();
        public List<RecentIdea> Recent { get; set; } = new();

        public record StatusCount(string Code, string Name, string Color, int Count);
        public record RecentIdea(string Reference, string Title, string StatusName, string StatusColor, string Domain, DateTime CreatedAt);
    }
}