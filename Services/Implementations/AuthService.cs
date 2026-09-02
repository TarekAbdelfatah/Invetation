using System.Security.Claims;
using Ibtikar.Data;
using Ibtikar.Models;
using Ibtikar.Services;
using Ibtikar.Services.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Services.Implementations
{
    public sealed class AuthService
    {
        private readonly IbtikarDbContext _db;
        private readonly Pbkdf2PasswordHasher _hasher;
        private readonly AuditLogService _audit;

        public AuthService(IbtikarDbContext db, Pbkdf2PasswordHasher hasher, AuditLogService audit)
        {
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
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, ct);

            var ok = user is not null && _hasher.Verify(password, user.PasswordSalt, user.PasswordHash);
            if (!ok)
                return LoginResult.Failed("اسم المستخدم أو كلمة المرور غير صحيحة.");

            return LoginResult.Success(user!);
        }

        public async Task SignInAsync(HttpContext httpContext, User user, CancellationToken ct = default)
        {
            if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));
            if (user is null) throw new ArgumentNullException(nameof(user));

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
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

            foreach (var ur in user.UserRoles)
            {
                var roleCode = ur.Role?.Code;
                if (string.IsNullOrEmpty(roleCode) || ur.Role is not { IsActive: true }) continue;
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
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(20)
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

        public async Task<User> SyncSsoUserAsync(DTOs.SSoUserInfo userInfo, CancellationToken ct = default)
        {
            if (userInfo is null) throw new ArgumentNullException(nameof(userInfo));

            var username = !string.IsNullOrWhiteSpace(userInfo.PreferredUsername)
                ? userInfo.PreferredUsername
                : (!string.IsNullOrWhiteSpace(userInfo.Email) ? userInfo.Email : userInfo.Sub);

            var user = await _db.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username, ct);

            if (user is null)
            {
                var fullName = !string.IsNullOrWhiteSpace(userInfo.Name)
                    ? userInfo.Name
                    : $"{userInfo.GivenName} {userInfo.FamilyName}".Trim();
                if (string.IsNullOrWhiteSpace(fullName)) fullName = username;

                user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = username,
                    FullName = fullName,
                    Email = userInfo.Email ?? string.Empty,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // Assign default role (Beneficiary / Employee)
                var defaultRole = await _db.Roles.FirstOrDefaultAsync(r => r.Code == RoleCodes.ExternalBeneficiary || r.Code == RoleCodes.InternalBeneficiary, ct);
                if (defaultRole is not null)
                {
                    user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = defaultRole.Id });
                }

                _db.Users.Add(user);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(userInfo.Name) && user.FullName != userInfo.Name)
                    user.FullName = userInfo.Name;
                if (!string.IsNullOrWhiteSpace(userInfo.Email) && user.Email != userInfo.Email)
                    user.Email = userInfo.Email;
            }

            var adminUser = await _db.Admins.FirstOrDefaultAsync(a => a.NetworkUser == username, ct);
            if (adminUser is null)
            {
                var defaultRole = await _db.Roles.FirstOrDefaultAsync(r => r.Code == RoleCodes.AuditEmployee || r.Code == RoleCodes.Admin, ct);
                if (defaultRole is not null)
                {
                    _db.Admins.Add(new Admin
                    {
                        NetworkUser = username,
                        RoleId = defaultRole.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return user;
        }

        public readonly record struct LoginResult(bool IsSuccess, string? ErrorMessage, User? User)
        {
            public static LoginResult Failed(string message) => new(false, message, null);
            public static LoginResult Success(User user) => new(true, null, user);
        }
    }
}

