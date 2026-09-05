using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Ibtikar.DTOs
{
    public class SSoUserInfo
    {
        [JsonPropertyName("sub")]
        public string Sub { get; set; } = string.Empty;

        [JsonPropertyName("UserCode")]
        public string UserCode { get; set; } = string.Empty;

        [JsonPropertyName("networkUser")]
        public string NetworkUser { get; set; } = string.Empty;

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("secondName")]
        public string SecondName { get; set; } = string.Empty;

        [JsonPropertyName("thirdName")]
        public string ThirdName { get; set; } = string.Empty;

        [JsonPropertyName("familyName")]
        public string FamilyName { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;

        [JsonPropertyName("identityNo")]
        public string IdentityNo { get; set; } = string.Empty;

        [JsonPropertyName("identityTypeId")]
        public string IdentityTypeId { get; set; } = string.Empty;

        [JsonPropertyName("identityStartDate")]
        public string IdentityStartDate { get; set; } = string.Empty;

        [JsonPropertyName("identityEndDate")]
        public string IdentityEndDate { get; set; } = string.Empty;

        [JsonPropertyName("birthDate")]
        public string BirthDate { get; set; } = string.Empty;

        [JsonPropertyName("GenderTypeDesc")]
        public string GenderTypeDesc { get; set; } = string.Empty;

        [JsonPropertyName("genderTypeId")]
        public string GenderTypeId { get; set; } = string.Empty;

        [JsonPropertyName("NationalityCode")]
        public string NationalityCode { get; set; } = string.Empty;

        [JsonPropertyName("NationalityDesc")]
        public string NationalityDesc { get; set; } = string.Empty;

        [JsonPropertyName("department_name")]
        public string DepartmentName { get; set; } = string.Empty;

        [JsonPropertyName("department_code")]
        public string DepartmentCode { get; set; } = string.Empty;

        [JsonPropertyName("isExternal")]
        public string? IsExternalCamelElement { get; set; }

   
        public string GetEffectiveFullName()
        {
            var composed = $"{FirstName} {SecondName} {ThirdName} {FamilyName}".Trim();
            composed = Regex.Replace(composed, @"\s+", " ");
            if (!string.IsNullOrWhiteSpace(composed)) return composed;
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            return GetEffectiveUsername();
        }

        public string GetEffectiveUsername()
        {
            if (!string.IsNullOrWhiteSpace(NetworkUser)) return NetworkUser;
            if (!string.IsNullOrWhiteSpace(Email)) return Email;
            return Sub;
        }

        public string GetEffectiveIdentityNo()
        {
            if (!string.IsNullOrWhiteSpace(IdentityNo)) return IdentityNo;
            return string.Empty;
        }
    }
}
