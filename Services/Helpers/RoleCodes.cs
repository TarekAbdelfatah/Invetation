namespace Ibtikar.Services.Helpers
{
    public static class RoleCodes
    {
        public const string AuditEmployee = "AuditEmployee";
        public const string SpecializedDepartment = "SpecializedDepartment";
        public const string PartnerDepartment = "SpecializedDepartment";
        public const string InnovationCommitteeMember = "InnovationCommitteeMember";
        public const string SystemAdmin = "admin";
        public const string ExternalBeneficiary = "ExternalBeneficiary";
        public const string InternalBeneficiary = "InternalBeneficiary";

        public sealed record HomeRoute(string Controller, string Action);

        public static readonly IReadOnlyDictionary<string, HomeRoute> HomeRedirects =
            new Dictionary<string, HomeRoute>(StringComparer.OrdinalIgnoreCase)
            {
                [AuditEmployee]             = new("Audit",                "Inbox"),
                [SpecializedDepartment]     = new("SpecializedDashboard", "Index"),
                [InnovationCommitteeMember] = new("CommitteeForMembers",  "Index"),
                [SystemAdmin]               = new("AdminOverview",        "Index"),
            };

        public static readonly HomeRoute DefaultBeneficiaryHome = new("MyRequests", "Index");

        public const string ClaimType = "ibtikar_role";
        public const string UserIdClaim = "ibtikar_user_id";
        public const string FullNameClaim = "ibtikar_full_name";
        public const string DepartmentIdClaim = "ibtikar_department_id";
        public const string DepartmentNameClaim = "ibtikar_department_name";
    }
}
