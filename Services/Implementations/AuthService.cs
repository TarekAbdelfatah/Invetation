using System.Security.Claims;
using Ibtikar.DTOs.Account;
using Ibtikar.Models;
using Ibtikar.Repositories;
using Ibtikar.Services;
using Ibtikar.Services.Helpers;
using Microsoft.AspNetCore.Authentication;

namespace Ibtikar.Services.Implementations
{
    public sealed class AuthService
    {
        private readonly IUserRepository _users;
        private readonly Pbkdf2PasswordHasher _hasher;
        private readonly AuditLogService _audit;

        public AuthService(IUserRepository users, Pbkdf2PasswordHasher hasher, AuditLogService audit)
        {
            _users = users;
            _hasher = hasher;
            _audit = audit;
        }

        public async Task<LoginResult> LoginAsync(string username, string password, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(username))
                return LoginResult.Failed("اسم المستخدم أو كلمة المرور غير صحيحة.");
            if (string.IsNullOrEmpty(password))
                return LoginResult.Failed("اسم المستخدم أو كلمة المرور غير صحيحة.");

            var user = await _users.GetActiveByUsernameWithRolesAsync(username, ct);

            var ok = user is not null && _hasher.Verify(password, user.PasswordSalt, user.PasswordHash);
            if (!ok)
                return LoginResult.Failed("اسم المستخدم أو كلمة المرور غير صحيحة.");

            return LoginResult.Success(user!);
        }

        public Task<IReadOnlyList<DemoUserDto>> GetDemoUsersAsync(CancellationToken ct = default)
            => _users.GetDemoUsersAsync(ct);

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
            await _users.SaveChangesAsync(ct);
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

        public readonly record struct LoginResult(bool IsSuccess, string? ErrorMessage, User? User)
        {
            public static LoginResult Failed(string message) => new(false, message, null);
            public static LoginResult Success(User user) => new(true, null, user);
        }
    }
}