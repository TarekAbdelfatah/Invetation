using System.ComponentModel.DataAnnotations;

namespace Ibtikar.ViewModels
{
    public record ErpEmployeeOption(string NetworkUser, string FullName);

    public record ErpDepartmentOption(int DeptId, string DeptName);

    public record RoleOptionDto(Guid RoleId, string Code, string Name);

    public record AdminRoleRowDto(
        int Id,
        string NetworkUser,
        string FullName,
        string RoleCode,
        string RoleName,
        int? DeptId,
        string? DeptName,
        bool IsActive,
        DateTime CreatedAt);

    public class AdminRoleIndexVm
    {
        public List<ErpEmployeeOption> Employees { get; set; } = new();
        public List<ErpDepartmentOption> Departments { get; set; } = new();
        public List<RoleOptionDto> Roles { get; set; } = new();
        public List<AdminRoleRowDto> AssignedAdmins { get; set; } = new();
        public AssignAdminRoleInputVm Input { get; set; } = new();
    }

    public class AssignAdminRoleInputVm
    {
        [Required(ErrorMessage = "يرجى اختيار الموظف.")]
        public string NetworkUser { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى اختيار الصلاحية.")]
        public Guid RoleId { get; set; }

        public int? DeptId { get; set; }
    }
}
