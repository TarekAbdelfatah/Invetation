using System.Security.Claims;
using Ibtikar.Data;
using Ibtikar.DTOs;
using Ibtikar.Models;
using Ibtikar.Repositories;
using Ibtikar.Services;
using Ibtikar.Services.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Services.Implementations
{
    public sealed class AuthService
    {
        private readonly IUserRepository _users;
        private readonly IbtikarDbContext _db;
        private readonly Pbkdf2PasswordHasher _hasher;
        private readonly AuditLogService _audit;

        public AuthService(IUserRepository users, IbtikarDbContext db, Pbkdf2PasswordHasher hasher, AuditLogService audit)
        {
            _users = users;
            _db = db;
            _hasher = hasher;
            _audit = audit;
        }

        public async Task<LoginResult> LoginAsync(string username, string password, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(username))
                return LoginResult.Failed("اسم المستخدم أو كلمة المرور غير صحيحة.");
            if (string.IsNullOrEmpty(password))
                return LoginResult.Failed("اسم المستخدم أو كلمة المرور غير صحيحة.");

            var user = await _db.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, ct);

            var ok = user is not null && _hasher.Verify(password, user.PasswordSalt, user.PasswordHash);
            if (!ok)
                return LoginResult.Failed("اسم المستخدم أو كلمة المرور غير صحيحة.");

            return LoginResult.Success(user!);
        }

        public async Task SignInAsync(HttpContext httpContext, User user, string roleCode = "", CancellationToken ct = default)
        {
            if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));
            if (user is null) throw new ArgumentNullException(nameof(user));

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new("preferred_username", user.Username),
                new("networkUser", user.Username),
                new("NetworkUser", user.Username),
                new(RoleCodes.UserIdClaim, user.Id.ToString()),
                new(RoleCodes.FullNameClaim, user.FullName)
            };

            if (user.DepartmentId.HasValue)
            {
                claims.Add(new Claim(RoleCodes.DepartmentIdClaim, user.DepartmentId.Value.ToString()));
                if (!string.IsNullOrWhiteSpace(user.Department?.Name))
                {
                    claims.Add(new Claim(RoleCodes.DepartmentNameClaim, user.Department.Name));
                }
            }

            if (!string.IsNullOrEmpty(roleCode))
            {
                claims.Add(new Claim(RoleCodes.ClaimType, roleCode));
                claims.Add(new Claim(ClaimTypes.Role, roleCode));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthExtensions.Scheme);
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync(
                CookieAuthExtensions.Scheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
                });

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

        public async Task SignOutAsync(HttpContext httpContext)
        {
            if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));
            await httpContext.SignOutAsync(CookieAuthExtensions.Scheme);
            await _audit.WriteAsync(
                action: "logout",
                entityName: nameof(User),
                entityId: null,
                newValues: null,
                oldValues: null,
                ct: httpContext.RequestAborted);
        }

        /// <summary>
        /// Syncs SSO user into Users table and resolves effective role without UserRole table dependency.
        /// 1. Internal User: Checks Admins table for Admin Role. If not found -> InternalBeneficiary.
        /// 2. External User: ExternalBeneficiary.
        /// 3. Saves/updates user info in Users table.
        /// </summary>
        public async Task<(User User, string RoleCode)> SyncSsoUserAsync(SSoUserInfo userInfo, CancellationToken ct = default)
        {
            if (userInfo is null) throw new ArgumentNullException(nameof(userInfo));

            var rawUsername = userInfo.GetEffectiveUsername();

            // 1. Strip @bog.gov.sa from username if present
            var username = rawUsername;
            if (!string.IsNullOrWhiteSpace(username) && username.EndsWith("@bog.gov.sa", StringComparison.OrdinalIgnoreCase))
            {
                username = username.Substring(0, username.Length - "@bog.gov.sa".Length);
            }

            var user = await _db.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username, ct);

            var fullName = userInfo.GetEffectiveFullName();

            // Save/Update in Users table
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
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(fullName) && user.FullName != fullName)
                    user.FullName = fullName;
                if (!string.IsNullOrWhiteSpace(userInfo.Email) && user.Email != userInfo.Email)
                    user.Email = userInfo.Email;
            }

            // Match department if provided
            if (!string.IsNullOrWhiteSpace(userInfo.DepartmentCode) || !string.IsNullOrWhiteSpace(userInfo.DepartmentName))
            {
                var dept = await _db.Departments.FirstOrDefaultAsync(d =>
                    (!string.IsNullOrEmpty(userInfo.DepartmentCode) && d.Code == userInfo.DepartmentCode) ||
                    (!string.IsNullOrEmpty(userInfo.DepartmentName) && d.Name == userInfo.DepartmentName), ct);
                if (dept is not null)
                {
                    user.DepartmentId = dept.Id;
                }
            }

            // 2. Role Determination without UserRole table dependency:
            string roleCode;
            if (userInfo.IsExternalUser)
            {
                // External User -> ExternalBeneficiary
                roleCode = RoleCodes.ExternalBeneficiary;
            }
            else
            {
                // Internal User -> Check Admins Table for admin Role
                var adminUser = await _db.Admins
                    .Include(a => a.Role)
                    .FirstOrDefaultAsync(a => a.NetworkUser == username && a.IsActive, ct);

                if (adminUser is not null && adminUser.Role is { IsActive: true })
                {
                    roleCode = adminUser.Role.Code;
                }
                else
                {
                    // If not found in Admins table, role is InternalBeneficiary
                    roleCode = RoleCodes.InternalBeneficiary;
                }
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return (user, roleCode);
        }

        public async Task<List<User>> GetDemoUsersAsync(CancellationToken ct = default)
        {
            return await _db.Users
                .Include(u => u.Department)
                .Where(u => u.IsActive)
                .OrderBy(u => u.Username)
                .ToListAsync(ct);
        }

        public readonly record struct LoginResult(bool IsSuccess, string? ErrorMessage, User? User)
        {
            public static LoginResult Failed(string message) => new(false, message, null);
            public static LoginResult Success(User user) => new(true, null, user);
        }
    }
}