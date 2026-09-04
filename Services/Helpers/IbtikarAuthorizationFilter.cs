using System.Security.Claims;
using Ibtikar.Data;
using Ibtikar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Services.Helpers
{
    public class IbtikarAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly string[] _requiredRoles;
        private const string _domainSuffix = "@bog.gov.sa";

        private static readonly HashSet<string> _beneficiaryRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            RoleCodes.ExternalBeneficiary,
            RoleCodes.InternalBeneficiary,
            "External-user",
            "Internal-user"
        };

        private static readonly string[] _networkUserClaimTypes =
        {
            "networkUser",
            "NetworkUser",
            "preferred_username",
            ClaimTypes.NameIdentifier,
            "sub",
            "upn"
        };

        public IbtikarAuthorizationFilter(string[] roles)
        {
            _requiredRoles = roles ?? Array.Empty<string>();
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any())
            {
                return;
            }

            var userPrincipal = context.HttpContext.User;

            if (userPrincipal.Identity is not { IsAuthenticated: true })
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Beneficiary logic: if no roles required or if any required role is a beneficiary role, allow access
            bool containsBeneficiaryRole = _requiredRoles.Length == 0 || _requiredRoles.Any(r => _beneficiaryRoles.Contains(r));

            if (containsBeneficiaryRole)
            {
                return;
            }

            var networkUser = ExtractNetworkUserClaim(userPrincipal);

            var db = context.HttpContext.RequestServices.GetRequiredService<IbtikarDbContext>();
            Admin? adminUser = null;
            if (!string.IsNullOrWhiteSpace(networkUser))
            {
                adminUser = await db.Admins
                    .AsNoTracking()
                    .Include(a => a.Role)
                    .FirstOrDefaultAsync(a => a.NetworkUser == networkUser && a.IsActive);
            }

            var roleCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (adminUser?.Role is { IsActive: true })
            {
                roleCodes.Add(adminUser.Role.Code);
            }

            foreach (var claim in userPrincipal.FindAll(RoleCodes.ClaimType).Concat(userPrincipal.FindAll(ClaimTypes.Role)))
            {
                if (!string.IsNullOrWhiteSpace(claim.Value))
                {
                    roleCodes.Add(claim.Value);
                }
            }

            bool hasExplicitRole = _requiredRoles.Any(r => roleCodes.Contains(r));
            if (!hasExplicitRole)
            {
                context.Result = new ForbidResult();
            }
        }

        private static string? ExtractNetworkUserClaim(ClaimsPrincipal principal)
        {
            string? raw = null;
            
            foreach (var claimType in _networkUserClaimTypes)
            {
                raw = principal.FindFirst(claimType)?.Value;
                if (!string.IsNullOrWhiteSpace(raw)) break;
            }

            raw ??= principal.Identity?.Name;

            if (!string.IsNullOrWhiteSpace(raw) && raw.EndsWith(_domainSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return raw.Substring(0, raw.Length - _domainSuffix.Length);
            }

            return raw;
        }
    }
}
