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

        public IbtikarAuthorizationFilter(string[] roles)
        {
            _requiredRoles = roles ?? Array.Empty<string>();
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // 1. AllowAnonymous Exemption Check
            if (context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any())
            {
                return;
            }

            var userPrincipal = context.HttpContext.User;

            // 2. Check if user is authenticated via IdentityServer / Auth Cookie
            if (userPrincipal.Identity is not { IsAuthenticated: true })
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // 3. For external or internal users (beneficiaries): check only if user is logged in
            bool containsBeneficiaryRole = _requiredRoles.Length == 0 || _requiredRoles.Any(r =>
                string.Equals(r, RoleCodes.ExternalBeneficiary, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r, RoleCodes.InternalBeneficiary, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r, "External-user", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r, "Internal-user", StringComparison.OrdinalIgnoreCase));

            // Extract Network User Claim from Token/Cookie
            var networkUser = ExtractNetworkUserClaim(userPrincipal);

            // 4. Resolve Database Context & Query Admins Table for Network User
            var db = context.HttpContext.RequestServices.GetRequiredService<IbtikarDbContext>();
            Admin? adminUser = null;
            if (!string.IsNullOrWhiteSpace(networkUser))
            {
                adminUser = await db.Admins
                    .AsNoTracking()
                    .Include(a => a.Role)
                    .FirstOrDefaultAsync(a => a.NetworkUser == networkUser && a.IsActive);
            }

            // 5. Collect Role Codes from Admin record & User Claims
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

            // 6. Role Authorization Check
            if (_requiredRoles.Length > 0)
            {
                // If endpoint accepts external or internal users and the user is logged in, allow access
                bool hasExplicitRole = _requiredRoles.Any(r => roleCodes.Contains(r));
                if (!containsBeneficiaryRole && !hasExplicitRole)
                {
                    // User does not have the required administrative role
                    context.Result = new ForbidResult();
                    return;
                }
            }

            // 7. Retrieve Department from CommonSysDB schema if DeptId is present
            if (adminUser?.DeptId.HasValue == true)
            {
                var commonDb = context.HttpContext.RequestServices.GetService<CommonSysDbContext>();
                if (commonDb is not null)
                {
                    try
                    {
                        var hrDept = await commonDb.HrDepartments
                            .AsNoTracking()
                            .FirstOrDefaultAsync(d => d.DeptId == adminUser.DeptId.Value);
                        if (hrDept is not null)
                        {
                            context.HttpContext.Items["CommonDepartment"] = hrDept;
                            context.HttpContext.Items["DepartmentName"] = hrDept.DeptName;
                        }
                    }
                    catch
                    {
                        // Ignore department resolution if HrDepartments table is unreachable
                    }
                }
            }

            // 8. Store resolved Admin, DeptId, and Role info in HttpContext.Items
            if (adminUser is not null)
            {
                context.HttpContext.Items["AdminUser"] = adminUser;
                context.HttpContext.Items["DeptId"] = adminUser.DeptId;
            }
            context.HttpContext.Items["DbUserRoles"] = roleCodes;
        }

        private static string? ExtractNetworkUserClaim(ClaimsPrincipal principal)
        {
            var raw = principal.FindFirst("networkUser")?.Value
                ?? principal.FindFirst("NetworkUser")?.Value
                ?? principal.FindFirst("preferred_username")?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? principal.FindFirst("sub")?.Value
                ?? principal.FindFirst("upn")?.Value
                ?? principal.Identity?.Name;

            if (!string.IsNullOrWhiteSpace(raw) && raw.EndsWith("@bog.gov.sa", StringComparison.OrdinalIgnoreCase))
            {
                return raw.Substring(0, raw.Length - "@bog.gov.sa".Length);
            }

            return raw;
        }
    }
}
