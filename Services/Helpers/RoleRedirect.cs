using System.Security.Claims;

namespace Ibtikar.Services.Helpers
{
    public static class RoleRedirect
    {
        public static string? ResolveHomeFor(ClaimsPrincipal user)
        {
            if (user.Identity?.IsAuthenticated != true) return null;
            var codes = user.FindAll(RoleCodes.ClaimType).Select(c => c.Value).ToList();
            return ResolveHomeFor(codes);
        }

        public static string? ResolveHomeFor(IList<string> roleCodes)
        {
            foreach (var code in roleCodes)
            {
                if (RoleCodes.HomeRedirects.TryGetValue(code, out var path))
                    return path;
            }
            return null;
        }
    }
}
