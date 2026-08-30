using System.Security.Claims;
using Ibtikar.Data;
using Ibtikar.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Services.Security
{
    public sealed class AuthService
    {
        private readonly IbtikarDbContext _db;
        private readonly Pbkdf2PasswordHasher _hasher;

        public AuthService(IbtikarDbContext db, Pbkdf2PasswordHasher hasher)
        {
            _db = db;
            _hasher = hasher;
        }

        public async Task<LoginResult> LoginAsync(string username, string password, CancellationToken ct = default)
        {
            Validation().Username(username).Password(password);

            var user = await _db.Users
                .AsNoTracking()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, ct);

            if (user is null)
                return LoginResult.Failed("اسم المستخدم أو كلمة المرور غير صحيحة.");

            if (!_hasher.Verify(password, user.PasswordSalt, user.PasswordHash))
                return LoginResult.Failed("اسم المستخدم أو كلمة المرور غير صحيحة.");

            return LoginResult.Success(user);
        }

        public async Task SignInAsync(HttpContext httpContext, User user, CancellationToken ct = default)
        {
            Validation().Context(httpContext).User(user);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(RoleCodes.UserIdClaim, user.Id.ToString()),
                new(RoleCodes.FullNameClaim, user.FullName)
            };

            foreach (var ur in user.UserRoles.Where(r => r.Role?.IsActive == true))
            {
                claims.Add(new Claim(RoleCodes.ClaimType, ur.Role!.Code));
                claims.Add(new Claim(ClaimTypes.Role, ur.Role!.Code));
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
            _db.Users.Update(user);
            await _db.SaveChangesAsync(ct);
        }

        public async Task SignOutAsync(HttpContext httpContext)
        {
            await httpContext.SignOutAsync(CookieAuthExtensions.Scheme);
        }

        public readonly record struct LoginResult(bool IsSuccess, string? ErrorMessage, User? User)
        {
            public static LoginResult Failed(string message) => new(false, message, null);
            public static LoginResult Success(User user) => new(true, null, user);
        }

        private static AuthValidator Validation() => new();

        private sealed class AuthValidator
        {
            public AuthValidator Username(string username)
            {
                if (string.IsNullOrWhiteSpace(username))
                    throw new ArgumentException("Username is required.", nameof(username));
                return this;
            }
            public AuthValidator Password(string password)
            {
                if (string.IsNullOrEmpty(password))
                    throw new ArgumentException("Password is required.", nameof(password));
                return this;
            }
            public AuthValidator Context(HttpContext ctx)
            {
                if (ctx is null) throw new ArgumentNullException(nameof(ctx));
                return this;
            }
            public AuthValidator User(User user)
            {
                if (user is null) throw new ArgumentNullException(nameof(user));
                return this;
            }
        }
    }
}
