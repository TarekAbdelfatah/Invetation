using System.Text.Json.Serialization;

namespace Ibtikar.DTOs
{
    public class SSoUserInfo
    {
        [JsonPropertyName("sub")]
        public string Sub { get; set; } = string.Empty;

        [JsonPropertyName("preferred_username")]
        public string PreferredUsername { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("given_name")]
        public string GivenName { get; set; } = string.Empty;

        [JsonPropertyName("family_name")]
        public string FamilyName { get; set; } = string.Empty;

        [JsonPropertyName("civil_id")]
        public string CivilId { get; set; } = string.Empty;

        [JsonPropertyName("employee_id")]
        public string EmployeeId { get; set; } = string.Empty;

        [JsonPropertyName("department_name")]
        public string DepartmentName { get; set; } = string.Empty;

        [JsonPropertyName("department_code")]
        public string DepartmentCode { get; set; } = string.Empty;

        [JsonPropertyName("user_type")]
        public string UserType { get; set; } = string.Empty;
    }
}
