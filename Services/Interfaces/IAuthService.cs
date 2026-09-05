using System.Security.Claims;
using Ibtikar.DTOs;
using Ibtikar.Models;

namespace Ibtikar.Services.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Syncs SSO user into Users table and determines their effective role.
        /// </summary>
        Task<(User User, string RoleCode)> SyncSsoUserAsync(SSoUserInfo userInfo, CancellationToken ct = default);

        /// <summary>
        /// Enriches the claims identity with user claims, resolves department, populates session, updates last login, and writes an audit log.
        /// </summary>
        Task EnrichClaimsAndSessionAsync(HttpContext httpContext, ClaimsIdentity identity, User user, string roleCode = "", string? idToken = null, CancellationToken ct = default);

        /// <summary>
        /// Signs the user into cookie authentication with enriched claims and stored tokens.
        /// </summary>
        Task SignInAsync(HttpContext httpContext, User user, string roleCode = "", string? idToken = null, int? expiresInSeconds = null, CancellationToken ct = default);

        /// <summary>
        /// Signs the user out of cookie authentication and records a logout audit log.
        /// </summary>
        Task SignOutAsync(HttpContext httpContext);

        /// <summary>
        /// Validates username and password for local/demo user login.
        /// </summary>
        Task<LoginResult> LoginAsync(string username, string password, CancellationToken ct = default);
    }

    public readonly record struct LoginResult(bool IsSuccess, string? ErrorMessage, User? User)
    {
        public static LoginResult Failed(string message) => new(false, message, null);
        public static LoginResult Success(User user) => new(true, null, user);
    }
}
