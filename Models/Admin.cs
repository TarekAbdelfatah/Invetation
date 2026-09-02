namespace Ibtikar.Models
{
    public class Admin
    {
        public int Id { get; set; }
        public string NetworkUser { get; set; } = string.Empty;
        public int? DeptId { get; set; }
        public Guid RoleId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Role? Role { get; set; }
    }
}
