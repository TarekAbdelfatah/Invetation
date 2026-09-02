using System.Security.Claims;
using Ibtikar.Data;
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

            // 2. Check if user is authenticated via IdentityServer
            if (userPrincipal.Identity is not { IsAuthenticated: true })
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // 3. Extract Network User Claim from Token
            var networkUser = ExtractNetworkUserClaim(userPrincipal);
            if (string.IsNullOrWhiteSpace(networkUser))
            {
                context.Result = new ForbidResult();
                return;
            }

            // 4. Resolve Database Context & Query Admins Table for Network User
            var db = context.HttpContext.RequestServices.GetRequiredService<IbtikarDbContext>();
            var adminUser = await db.Admins
                .AsNoTracking()
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.NetworkUser == networkUser && a.IsActive);

            if (adminUser is null)
            {
                // User does not exist in Admins table or is deactivated
                context.Result = new ForbidResult();
                return;
            }

            // 5. Extract Role Code from Admin's Role
            var adminRoleCode = adminUser.Role is { IsActive: true } ? adminUser.Role.Code : string.Empty;
            var roleCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(adminRoleCode))
            {
                roleCodes.Add(adminRoleCode);
            }

            // 6. Validate Required Roles on Endpoint against Admin Role
            if (_requiredRoles.Length > 0)
            {
                bool hasPermission = _requiredRoles.Any(r => roleCodes.Contains(r));
                if (!hasPermission)
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }

            // 7. Retrieve Department from CommonSysDB schema if DeptId is present
            if (adminUser.DeptId.HasValue)
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
            context.HttpContext.Items["AdminUser"] = adminUser;
            context.HttpContext.Items["DeptId"] = adminUser.DeptId;
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
