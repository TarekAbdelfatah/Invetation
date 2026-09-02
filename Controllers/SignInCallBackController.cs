using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Implementations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ibtikar.Controllers
{
    /// <summary>
    /// Explicit Custom OAuth2 / PKCE Flow Controller for IdentityServer integration.
    /// Handles manual authorization URL building, code exchange, user syncing, and local cookie session storage.
    /// </summary>
    [AllowAnonymous]
    public class SignInCallBackController : Controller
    {
        private const string OidcStateCookieName = ".Ibtikar.OidcState";
        private readonly ILogger<SignInCallBackController> _logger;
        private readonly SsoService _ssoService;
        private readonly AuthService _authService;

        public SignInCallBackController(
            ILogger<SignInCallBackController> logger,
            SsoService ssoService,
            AuthService authService)
        {
            _logger = logger;
            _ssoService = ssoService;
            _authService = authService;
        }

        /// <summary>
        /// Step 1: Initiates SSO Login by generating PKCE verifier, state, building the authorization URL,
        /// storing transient state in cookie, and redirecting browser to IdentityServer.
        /// </summary>
        [HttpGet("/Account/Login")]
        [HttpGet("/Login")]
        public IActionResult Login(string? returnUrl = "/")
        {
            var traceId = HttpContext.TraceIdentifier;
            _logger.LogInformation("[TraceId:{TraceId}] Initiating explicit OAuth2 PKCE login flow.", traceId);

            var (codeVerifier, codeChallenge) = GeneratePkce();
            var state = Guid.NewGuid().ToString("N");
            var nonce = Guid.NewGuid().ToString("N");

            // Save state and verifier in temporary HTTP-only cookie
            var statePayload = JsonSerializer.Serialize(new OidcStateData
            {
                State = state,
                CodeVerifier = codeVerifier,
                ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl
            });

            Response.Cookies.Append(OidcStateCookieName, statePayload, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddMinutes(15)
            });

            var redirectUri = BuildCallbackUrl();
            var authorizeUrl = _ssoService.BuildAuthorizeUrl(redirectUri, state, nonce, codeChallenge);

            _logger.LogInformation("[TraceId:{TraceId}] Redirecting to IdentityServer authorize endpoint. RedirectUri:{RedirectUri}",
                traceId, redirectUri);

            return Redirect(authorizeUrl);
        }

        /// <summary>
        /// Step 2 & 3 & 4: Callback handler for IdentityServer authorization code response.
        /// Validates state, exchanges code for tokens, retrieves user profile, syncs database user,
        /// and establishes local auth cookie session.
        /// </summary>
        [HttpGet("/signin-callback")]
        public async Task<IActionResult> SignInCallback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, [FromQuery] string? error_description)
        {
            var traceId = HttpContext.TraceIdentifier;
            _logger.LogInformation("[TraceId:{TraceId}] Entering /signin-callback handler.", traceId);

            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogError("[TraceId:{TraceId}] IdentityServer returned error: {Error} - {Description}", traceId, error, error_description);
                return RedirectToAction("Index", "Home", new { error = error_description ?? error });
            }

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            {
                _logger.LogWarning("[TraceId:{TraceId}] Callback missing code or state query parameters.", traceId);
                return RedirectToAction(nameof(Login));
            }

            // Retrieve stored state from cookie
            if (!Request.Cookies.TryGetValue(OidcStateCookieName, out var cookiePayload) || string.IsNullOrWhiteSpace(cookiePayload))
            {
                _logger.LogError("[TraceId:{TraceId}] OidcState cookie missing or expired. Correlation failed.", traceId);
                return RedirectToAction("Index", "Home", new { error = "correlation_failed" });
            }

            OidcStateData? stateData = null;
            try
            {
                stateData = JsonSerializer.Deserialize<OidcStateData>(cookiePayload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId:{TraceId}] Failed to deserialize OidcState cookie payload.", traceId);
            }

            if (stateData is null || !string.Equals(stateData.State, state, StringComparison.Ordinal))
            {
                _logger.LogError("[TraceId:{TraceId}] Mismatched state parameter. State validation failed.", traceId);
                Response.Cookies.Delete(OidcStateCookieName);
                return RedirectToAction("Index", "Home", new { error = "state_mismatch" });
            }

            // Delete transient state cookie
            Response.Cookies.Delete(OidcStateCookieName);

            var redirectUri = BuildCallbackUrl();
            try
            {
                _logger.LogInformation("[TraceId:{TraceId}] Exchanging authorization code for tokens...", traceId);
                var tokenResult = await _ssoService.ExchangeCodeForTokenDetailsAsync(code, redirectUri, stateData.CodeVerifier);

                if (string.IsNullOrWhiteSpace(tokenResult.AccessToken))
                {
                    _logger.LogError("[TraceId:{TraceId}] Token exchange returned empty access token.", traceId);
                    return RedirectToAction("Index", "Home", new { error = "empty_token" });
                }

                _logger.LogInformation("[TraceId:{TraceId}] Token exchange succeeded. Fetching UserInfo profile...", traceId);
                var userInfo = await _ssoService.GetSSOUserInfoAsync(tokenResult.AccessToken);

                if (userInfo is null)
                {
                    _logger.LogError("[TraceId:{TraceId}] UserInfo request returned null profile.", traceId);
                    return RedirectToAction("Index", "Home", new { error = "userinfo_failed" });
                }

                _logger.LogInformation("[TraceId:{TraceId}] UserInfo fetched for Username:{Username}. Syncing DB user...", traceId, userInfo.GetEffectiveUsername());
                var (user, roleCode) = await _authService.SyncSsoUserAsync(userInfo);

                _logger.LogInformation("[TraceId:{TraceId}] User synced in Users table with Role:{RoleCode}. Signing in user to local auth cookie...", traceId, roleCode);
                await _authService.SignInAsync(HttpContext, user, roleCode);

                // Store access_token in AuthenticationProperties for the session if needed
                var props = new AuthenticationProperties();
                props.StoreTokens(new[]
                {
                    new AuthenticationToken { Name = "access_token", Value = tokenResult.AccessToken },
                    new AuthenticationToken { Name = "id_token", Value = tokenResult.IdentityToken ?? string.Empty }
                });

                var returnUrl = stateData.ReturnUrl;
                if (string.IsNullOrWhiteSpace(returnUrl) || returnUrl == "/")
                {
                    if (RoleCodes.HomeRedirects.TryGetValue(roleCode, out var redirectPath))
                    {
                        returnUrl = redirectPath;
                    }
                    else
                    {
                        returnUrl = "/MyRequests";
                    }
                }
                _logger.LogInformation("[TraceId:{TraceId}] SSO authentication workflow completed successfully. Redirecting to {ReturnUrl}", traceId, returnUrl);

                return Redirect(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId:{TraceId}] Exception occurred during signin-callback code exchange or user sync: {Message}", traceId, ex.Message);
                return RedirectToAction("Index", "Home", new { error = Uri.EscapeDataString(ex.Message) });
            }
        }

        /// <summary>
        /// Step 5: Logout handler that clears local auth cookie and redirects to IdentityServer end-session endpoint.
        /// </summary>
        [HttpGet("/Logout")]
        public async Task<IActionResult> Logout()
        {
            var traceId = HttpContext.TraceIdentifier;
            _logger.LogInformation("[TraceId:{TraceId}] Initiating logout workflow.", traceId);

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            var postLogoutRedirectUri = $"{Request.Scheme}://{Request.Host}/";
            var logoutUrl = _ssoService.BuildLogoutUrl(postLogoutRedirectUri);

            _logger.LogInformation("[TraceId:{TraceId}] Local cookie cleared. Redirecting to IdentityServer logout URL: {LogoutUrl}", traceId, logoutUrl);
            return Redirect(logoutUrl);
        }

        private string BuildCallbackUrl()
        {
            return $"{Request.Scheme}://{Request.Host}/signin-callback";
        }

        private static (string codeVerifier, string codeChallenge) GeneratePkce()
        {
            byte[] randomBytes = new byte[32];
            RandomNumberGenerator.Fill(randomBytes);
            string codeVerifier = Base64UrlTextEncoder.Encode(randomBytes);

            byte[] challengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
            string codeChallenge = Base64UrlTextEncoder.Encode(challengeBytes);

            return (codeVerifier, codeChallenge);
        }

        private sealed class OidcStateData
        {
            public string State { get; set; } = string.Empty;
            public string CodeVerifier { get; set; } = string.Empty;
            public string ReturnUrl { get; set; } = string.Empty;
        }
    }
}
