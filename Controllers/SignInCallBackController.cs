using Ibtikar.Services.Helpers;
using Ibtikar.Services.Implementations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using Ibtikar.Models;
using Ibtikar.DTOs;
using System.Text.Json;

namespace Ibtikar.Controllers
{

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

        [HttpGet("/Account/Login")]
        [HttpGet("/Login")]
        public IActionResult Login(string? returnUrl = "/")
        {
            _logger.LogInformation("Initiating explicit OAuth2 PKCE login flow.");

            var (codeVerifier, codeChallenge) = GeneratePkce();
            var state = Guid.NewGuid().ToString("N");
            var nonce = Guid.NewGuid().ToString("N");

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

            return Redirect(authorizeUrl);
        }

        [HttpGet("/signin-callback")]
        public async Task<IActionResult> SignInCallback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, [FromQuery] string? error_description)
        {
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogError("IdentityServer returned error: {Error} - {Description}", error, error_description);
                return RedirectToAction("Index", "Home", new { error = error_description ?? error });
            }

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
                return RedirectToAction(nameof(Login));

            var (isSuccess, stateData, errorResult) = ValidateAndConsumeStateCookie(state);
            if (!isSuccess) return errorResult!;

            try
            {
                var tokenResult = await _ssoService.ExchangeCodeForTokenDetailsAsync(code, BuildCallbackUrl(), stateData!.CodeVerifier);
                if (string.IsNullOrWhiteSpace(tokenResult.AccessToken))
                {
                    _logger.LogError("Token exchange returned empty access token.");
                    return RedirectToAction("Index", "Home", new { error = "empty_token" });
                }

                var userInfo = await _ssoService.GetSSOUserInfoAsync(tokenResult.AccessToken);
                if (userInfo is null)
                {
                    _logger.LogError("UserInfo request returned null profile.");
                    return RedirectToAction("Index", "Home", new { error = "userinfo_failed" });
                }

                var (user, roleCode) = await _authService.SyncSsoUserAsync(userInfo);

                await EstablishLocalSessionAsync(user, roleCode, tokenResult);

                return RedirectToTargetUrl(stateData.ReturnUrl, roleCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during signin-callback code exchange or user sync: {Message}", ex.Message);
                return RedirectToAction("Index", "Home", new { error = Uri.EscapeDataString(ex.Message) });
            }
        }

        private (bool IsSuccess, OidcStateData? StateData, IActionResult? ErrorResult) ValidateAndConsumeStateCookie(string incomingState)
        {
            if (!Request.Cookies.TryGetValue(OidcStateCookieName, out var cookiePayload) || string.IsNullOrWhiteSpace(cookiePayload))
            {
                _logger.LogError("OidcState cookie missing or expired. Correlation failed.");
                return (false, null, RedirectToAction("Index", "Home", new { error = "correlation_failed" }));
            }

            OidcStateData? stateData = null;
            try
            {
                stateData = JsonSerializer.Deserialize<OidcStateData>(cookiePayload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize OidcState cookie payload.");
            }

            if (stateData is null || !string.Equals(stateData.State, incomingState, StringComparison.Ordinal))
            {
                _logger.LogError("Mismatched state parameter. State validation failed.");
                Response.Cookies.Delete(OidcStateCookieName);
                return (false, null, RedirectToAction("Index", "Home", new { error = "state_mismatch" }));
            }

            Response.Cookies.Delete(OidcStateCookieName);
            return (true, stateData, null);
        }

        private async Task EstablishLocalSessionAsync(User user, string roleCode, SsoTokenResult tokenResult)
        {
            if (!string.IsNullOrWhiteSpace(tokenResult.IdentityToken))
            {
                Response.Cookies.Append("id_token", tokenResult.IdentityToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = tokenResult.ExpiresIn > 0 
                        ? DateTimeOffset.UtcNow.AddSeconds(tokenResult.ExpiresIn) 
                        : DateTimeOffset.UtcNow.AddHours(8)
                });
            }

            await _authService.SignInAsync(HttpContext, user, roleCode, tokenResult.IdentityToken, tokenResult.ExpiresIn);
        }

        private IActionResult RedirectToTargetUrl(string returnUrl, string roleCode)
        {
            if (string.IsNullOrWhiteSpace(returnUrl) || returnUrl == "/")
            {
                if (RoleCodes.HomeRedirects.TryGetValue(roleCode, out var route))
                {
                    _logger.LogInformation("SSO authentication workflow completed successfully. Redirecting to role home.");
                    return RedirectToAction(route.Action, route.Controller);
                }

                _logger.LogInformation("SSO authentication workflow completed successfully. Redirecting to default beneficiary home.");
                return RedirectToAction(RoleCodes.DefaultBeneficiaryHome.Action, RoleCodes.DefaultBeneficiaryHome.Controller);
            }

            _logger.LogInformation("SSO authentication workflow completed successfully. Redirecting to {ReturnUrl}", returnUrl);

            if (returnUrl.StartsWith("/") && !returnUrl.StartsWith(Request.PathBase.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            {
                returnUrl = Request.PathBase + returnUrl;
            }
            return Redirect(returnUrl);
        }

        [HttpGet("/Logout")]
        [HttpGet("/SSO/Logout")]
        public async Task<IActionResult> Logout()
        {
            var idTokenHint = User.FindFirst("id_token")?.Value
                ?? await HttpContext.GetTokenAsync("id_token")
                ?? Request.Cookies["id_token"];

            var postLogoutRedirectUri = _ssoService.GetPostLogoutRedirectUri() 
                ?? $"https://{Request.Host}{Request.PathBase}/signout-callback-oidc";
            
            var logoutUrl = _ssoService.BuildLogoutUrl(postLogoutRedirectUri, idTokenHint);

            await AuthCookieClearer.ClearAsync(HttpContext, _logger);

            _logger.LogInformation("Local cookies & sessions cleared. Redirecting to IdentityServer logout URL: {LogoutUrl}", logoutUrl);
            return Redirect(logoutUrl);
        }

        private string BuildCallbackUrl()
        {
            var configured = _ssoService.GetRedirectUri();
            if (!string.IsNullOrWhiteSpace(configured) && configured.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return configured;
            }
            return $"https://{Request.Host}/signin-callback";
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
