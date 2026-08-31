namespace Ibtikar.Services.Security
{
    public static class RoleCodes
    {
        public const string AuditEmployee = "audit-employee";
        public const string SpecializedDepartment = "specialized-department";
        public const string PartnerDepartment = "partner-department";
        public const string InnovationCommitteeMember = "innovation-committee-member";
        public const string SystemAdmin = "system-admin";
        public const string ExternalBeneficiary = "external-beneficiary";
        public const string InternalBeneficiary = "internal-beneficiary";

        public static readonly IReadOnlyDictionary<string, string> HomeRedirects =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [AuditEmployee]            = "/Audit/Inbox",
                [SpecializedDepartment]    = "/SpecializedDashboard",
                [PartnerDepartment]        = "/PartnerDashboard",
                [InnovationCommitteeMember]= "/Committee",
                [SystemAdmin]              = "/AdminOverview",
                [ExternalBeneficiary]      = "/MyRequests",
                [InternalBeneficiary]      = "/MyRequests"
            };

        public const string ClaimType = "ibtikar_role";
        public const string UserIdClaim = "ibtikar_user_id";
        public const string FullNameClaim = "ibtikar_full_name";
        public const string DepartmentIdClaim = "ibtikar_department_id";
    }
}
