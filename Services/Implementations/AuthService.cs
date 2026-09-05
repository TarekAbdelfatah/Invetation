using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ibtikar.Data;
using Ibtikar.DTOs;
using Ibtikar.Models;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Services.Implementations
{
    public sealed class AuthService : IAuthService
    {
        private readonly IbtikarDbContext _db;
        private readonly Pbkdf2PasswordHasher _hasher;
        private readonly AuditLogService _audit;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IbtikarDbContext db,
            Pbkdf2PasswordHasher hasher,
            AuditLogService audit,
            ILogger<AuthService> logger)
        {
            _db = db;
            _hasher = hasher;
            _audit = audit;
            _logger = logger;
        }

        public async Task<LoginResult> LoginAsync(string username, string password, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                return LoginResult.Failed("اسم المستخدم أو كلمة المرور غير صحيحة.");

            var user = await _db.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, ct);

            var ok = user is not null && _hasher.Verify(password, user.PasswordSalt, user.PasswordHash);
            if (!ok)
                return LoginResult.Failed("اسم المستخدم أو كلمة المرور غير صحيحة.");

            return LoginResult.Success(user!);
        }

        /// <summary>
        /// Synchronizes user information from the IdentityServer SSO userinfo endpoint into the local database
        /// and determines their effective role within the system.
        /// </summary>
        public async Task<(User User, string RoleCode)> SyncSsoUserAsync(SSoUserInfo userInfo, CancellationToken ct = default)
        {
            if (userInfo is null) throw new ArgumentNullException(nameof(userInfo));

            var username = NormalizeUsername(userInfo.GetEffectiveUsername());
            var fullName = userInfo.GetEffectiveFullName();

            _logger.LogInformation("Synchronizing SSO profile for username: {Username}", username);

            var user = await _db.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username, ct);

            if (user is null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = username,
                    FullName = fullName,
                    Email = userInfo.Email ?? string.Empty,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Users.Add(user);
                _logger.LogInformation("Created new local User record for {Username} with ID {UserId}", username, user.Id);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(fullName) && user.FullName != fullName)
                    user.FullName = fullName;

                if (!string.IsNullOrWhiteSpace(userInfo.Email) && user.Email != userInfo.Email)
                    user.Email = userInfo.Email;
            }

            await AssociateDepartmentAsync(user, userInfo, ct);

            var isExternal = IsExternalUser(userInfo);
            var roleCode = await DetermineUserRoleAsync(username, isExternal, ct);

            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("User {Username} synchronized successfully with effective role {RoleCode}", username, roleCode);

            return (user, roleCode);
        }

        /// <summary>
        /// Populates claims, department claims, session values, updates the last login timestamp, and records an audit log.
        /// </summary>
        public async Task EnrichClaimsAndSessionAsync(
            HttpContext httpContext,
            ClaimsIdentity identity,
            User user,
            string roleCode = "",
            string? idToken = null,
            CancellationToken ct = default)
        {
            if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));
            if (identity is null) throw new ArgumentNullException(nameof(identity));
            if (user is null) throw new ArgumentNullException(nameof(user));

            var (deptIdStr, deptName) = await ResolveDepartmentClaimsAsync(user, roleCode, ct);

            AddClaimsToIdentity(identity, user, roleCode, idToken, deptIdStr, deptName);
            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            await _audit.WriteAsync(
                action: "login",
                entityName: nameof(User),
                entityId: user.Id.ToString(),
                newValues: null,
                oldValues: null,
                ct: ct);
        }

        /// <summary>
        /// Signs the user into cookie authentication using enriched claims.
        /// </summary>
        public async Task SignInAsync(
            HttpContext httpContext,
            User user,
            string roleCode = "",
            string? idToken = null,
            int? expiresInSeconds = null,
            CancellationToken ct = default)
        {
            if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));
            if (user is null) throw new ArgumentNullException(nameof(user));

            var identity = new ClaimsIdentity(CookieAuthExtensions.Scheme);
            await EnrichClaimsAndSessionAsync(httpContext, identity, user, roleCode, idToken, ct);

            var principal = new ClaimsPrincipal(identity);
            var authProps = new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = expiresInSeconds.HasValue && expiresInSeconds.Value > 0
                    ? DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds.Value)
                    : DateTimeOffset.UtcNow.AddHours(8)
            };

            if (!string.IsNullOrWhiteSpace(idToken))
            {
                authProps.StoreTokens(new[]
                {
                    new AuthenticationToken { Name = "id_token", Value = idToken }
                });
            }

            await httpContext.SignInAsync(
                CookieAuthExtensions.Scheme,
                principal,
                authProps);

            _logger.LogInformation("User {Username} signed into cookie authentication scheme {Scheme}", user.Username, CookieAuthExtensions.Scheme);
        }

        /// <summary>
        /// Signs the user out of cookie authentication and records a logout audit log.
        /// </summary>
        public async Task SignOutAsync(HttpContext httpContext)
        {
            if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));

            var userId = httpContext.User.FindFirst(RoleCodes.UserIdClaim)?.Value
                ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            await httpContext.SignOutAsync(CookieAuthExtensions.Scheme);

            await _audit.WriteAsync(
                action: "logout",
                entityName: nameof(User),
                entityId: userId,
                newValues: null,
                oldValues: null,
                ct: httpContext.RequestAborted);

            _logger.LogInformation("User {UserId} signed out of cookie authentication", userId ?? "Unknown");
        }

        #region Private Helpers

        private static string NormalizeUsername(string rawUsername)
        {
            if (string.IsNullOrWhiteSpace(rawUsername)) return string.Empty;

            const string domainSuffix = "@bog.gov.sa";
            if (rawUsername.EndsWith(domainSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return rawUsername[..^domainSuffix.Length];
            }
            return rawUsername.Trim();
        }

        private static bool IsExternalUser(SSoUserInfo userInfo)
        {
            if (string.IsNullOrWhiteSpace(userInfo.IsExternalCamelElement)) return false;

            var val = userInfo.IsExternalCamelElement.Trim();
            return string.Equals(val, "1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(val, "true", StringComparison.OrdinalIgnoreCase);
        }

        private async Task AssociateDepartmentAsync(User user, SSoUserInfo userInfo, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(userInfo.DepartmentCode) && string.IsNullOrWhiteSpace(userInfo.DepartmentName))
                return;

            var dept = await _db.Departments.FirstOrDefaultAsync(d =>
                (!string.IsNullOrEmpty(userInfo.DepartmentCode) && d.Code == userInfo.DepartmentCode) ||
                (!string.IsNullOrEmpty(userInfo.DepartmentName) && d.Name == userInfo.DepartmentName), ct);

            if (dept is not null)
            {
                user.DepartmentId = dept.Id;
            }
        }

        private async Task<string> DetermineUserRoleAsync(string username, bool isExternal, CancellationToken ct)
        {
            if (isExternal)
            {
                return RoleCodes.ExternalBeneficiary;
            }

            // Check Admins table for assigned active role
            var adminUser = await _db.Admins
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.NetworkUser == username && a.IsActive, ct);

            if (adminUser?.Role is { IsActive: true })
            {
                return adminUser.Role.Code;
            }

            // Check InnovationCommitteeMember table
            var networkUserGuid = NetworkUserToGuid(username);
            var isCommitteeMember = await _db.CommitteeMembers
                .AsNoTracking()
                .AnyAsync(m => m.UserId == networkUserGuid, ct);

            if (isCommitteeMember)
            {
                return RoleCodes.InnovationCommitteeMember;
            }

            return RoleCodes.InternalBeneficiary;
        }

        private async Task<(string? DeptIdStr, string? DeptName)> ResolveDepartmentClaimsAsync(
            User user,
            string roleCode,
            CancellationToken ct)
        {
            string? resolvedDeptIdStr = user.DepartmentId?.ToString();
            string? resolvedDeptName = user.Department?.Name;

            if (string.Equals(roleCode, RoleCodes.SpecializedDepartment, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(roleCode, RoleCodes.PartnerDepartment, StringComparison.OrdinalIgnoreCase))
            {
                var adminUser = await _db.Admins
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.NetworkUser == user.Username && a.IsActive, ct);

                if (adminUser?.DeptId.HasValue == true)
                {
                    var codeStr = adminUser.DeptId.Value.ToString();
                    var dept = await _db.Departments
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.Code == codeStr, ct);

                    if (dept != null)
                    {
                        resolvedDeptIdStr = dept.Id.ToString();
                        resolvedDeptName = dept.Name;
                    }
                    else
                    {
                        resolvedDeptIdStr = codeStr;
                    }
                }
            }

            return (resolvedDeptIdStr, resolvedDeptName);
        }

        private static void AddClaimsToIdentity(
            ClaimsIdentity identity,
            User user,
            string roleCode,
            string? idToken,
            string? deptIdStr,
            string? deptName)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new("preferred_username", user.Username),
                new("networkUser", user.Username),
                new("NetworkUser", user.Username),
                new(RoleCodes.UserIdClaim, user.Id.ToString()),
                new(RoleCodes.FullNameClaim, user.FullName ?? string.Empty)
            };

            if (!string.IsNullOrWhiteSpace(idToken) && !identity.HasClaim(c => c.Type == "id_token"))
            {
                claims.Add(new Claim("id_token", idToken));
            }

            if (!string.IsNullOrWhiteSpace(deptIdStr))
            {
                claims.Add(new Claim(RoleCodes.DepartmentIdClaim, deptIdStr));
            }

            if (!string.IsNullOrWhiteSpace(deptName))
            {
                claims.Add(new Claim(RoleCodes.DepartmentNameClaim, deptName));
            }

            if (!string.IsNullOrEmpty(roleCode))
            {
                claims.Add(new Claim(RoleCodes.ClaimType, roleCode));
                claims.Add(new Claim(ClaimTypes.Role, roleCode));
            }

            foreach (var claim in claims)
            {
                if (!identity.HasClaim(c => c.Type == claim.Type && c.Value == claim.Value))
                {
                    identity.AddClaim(claim);
                }
            }
        }

        
        private static Guid NetworkUserToGuid(string networkUser)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(networkUser));
            var bytes = new byte[16];
            Array.Copy(hash, bytes, 16);
            return new Guid(bytes);
        }

        #endregion
    }
}