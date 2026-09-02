namespace Ibtikar.Services.Helpers
{
    public static class RoleCodes
    {
        public const string AuditEmployee = "AuditEmployee";
        public const string SpecializedDepartment = "SpecializedDepartment";
        public const string PartnerDepartment = "SpecializedDepartment";
        public const string InnovationCommitteeMember = "InnovationCommitteeMember";
        public const string SystemAdmin = "admin";
        public const string Admin = "admin";
        public const string ExternalBeneficiary = "ExternalBeneficiary";
        public const string InternalBeneficiary = "InternalBeneficiary";

        public static readonly IReadOnlyDictionary<string, string> HomeRedirects =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [AuditEmployee]            = "/Audit/Inbox",
                [SpecializedDepartment]    = "/SpecializedDashboard",
                [InnovationCommitteeMember]= "/Committee",
                [SystemAdmin]              = "/AdminOverview"
            };

        public const string ClaimType = "ibtikar_role";
        public const string UserIdClaim = "ibtikar_user_id";
        public const string FullNameClaim = "ibtikar_full_name";
        public const string DepartmentIdClaim = "ibtikar_department_id";
        public const string DepartmentNameClaim = "ibtikar_department_name";
    }
}
