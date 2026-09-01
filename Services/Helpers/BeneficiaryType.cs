using System.Security.Claims;
using Ibtikar.Services.Helpers;

namespace Ibtikar.Services.Helpers
{
    /// <summary>
    /// Single source of truth for distinguishing an internal beneficiary from an
    /// external one. The check is anchored on the <see cref="RoleCodes.DepartmentIdClaim"/>
    /// claim set by <see cref="Ibtikar.Services.Security.AuthService"/> at login.
    /// Role codes are used only by <c>[Authorize]</c> attributes and by the home
    /// redirect table; do not branch on them to determine internal/external.
    /// </summary>
    public static class BeneficiaryType
    {
        public static bool IsInternal(ClaimsPrincipal? user)
        {
            if (user is null) return false;
            var raw = user.FindFirst(RoleCodes.DepartmentIdClaim)?.Value;
            return !string.IsNullOrEmpty(raw) && Guid.TryParse(raw, out _);
        }

        public static bool IsExternal(ClaimsPrincipal? user)
            => !IsInternal(user);
    }
}
