using Ibtikar.Services.Implementations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ibtikar.Controllers
{
    [AllowAnonymous]
    public class SignInCallBackController : Controller
    {
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
        public IActionResult Login()
        {
            var traceId = HttpContext.TraceIdentifier;
            _logger.LogInformation("[TraceId:{TraceId}] Initiating SSO login challenge. Redirecting to /signin-complete post-OIDC authentication.", traceId);

            var props = new AuthenticationProperties
            {
                RedirectUri = "/signin-complete"
            };

            return Challenge(props, "oidc");
        }

        /// <summary>
        /// Explicit action handler for /signin-callback endpoint.
        /// Handles direct calls or fallback processing for the OIDC sign-in callback.
        /// </summary>
        [HttpGet("/signin-callback")]
        [HttpPost("/signin-callback")]
        public async Task<IActionResult> SignInCallback()
        {
            var traceId = HttpContext.TraceIdentifier;
            _logger.LogInformation("[TraceId:{TraceId}] Entering /signin-callback action handler.", traceId);

            try
            {
                var authResult = await HttpContext.AuthenticateAsync("Cookies");
                if (!authResult.Succeeded)
                {
                    authResult = await HttpContext.AuthenticateAsync("oidc");
                }

                if (authResult.Succeeded && authResult.Principal != null)
                {
                    var accessToken = authResult.Properties?.GetTokenValue("access_token")
                        ?? await HttpContext.GetTokenAsync("access_token");

                    if (!string.IsNullOrWhiteSpace(accessToken))
                    {
                        var userInfo = await _ssoService.GetSSOUserInfoAsync(accessToken);
                        if (userInfo != null)
                        {
                            var user = await _authService.SyncSsoUserAsync(userInfo);
                            await _authService.SignInAsync(HttpContext, user);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId:{TraceId}] Error in /signin-callback action handler.", traceId);
            }

            return RedirectToAction(nameof(SigninComplete));
        }

        /// <summary>
        /// Called after the OIDC middleware has processed the SSO callback (/signin-callback) and signed the user in.
        /// Fetches user info from SSO, saves or updates the user in DB, and establishes local cookie session.
        /// </summary>
        [HttpGet("/signin-complete")]
        public async Task<IActionResult> SigninComplete()
        {
            var traceId = HttpContext.TraceIdentifier;
            _logger.LogInformation("[TraceId:{TraceId}] Entering /signin-complete callback handler.", traceId);

            try
            {
                var accessToken = await HttpContext.GetTokenAsync("access_token");
                _logger.LogInformation("[TraceId:{TraceId}] Retrieved access_token from HttpContext. TokenPresent:{HasToken}",
                    traceId, !string.IsNullOrWhiteSpace(accessToken));

                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    _logger.LogInformation("[TraceId:{TraceId}] Fetching SSO UserInfo from IdentityServer...", traceId);
                    var userInfo = await _ssoService.GetSSOUserInfoAsync(accessToken);

                    if (userInfo != null)
                    {
                        _logger.LogInformation("[TraceId:{TraceId}] SSO UserInfo retrieved successfully for Username:{Username}, Sub:{Sub}, Email:{Email}",
                            traceId, userInfo.PreferredUsername, userInfo.Sub, userInfo.Email);

                        var user = await _authService.SyncSsoUserAsync(userInfo);
                        _logger.LogInformation("[TraceId:{TraceId}] Synced user in database. UserId:{UserId}, Username:{Username}",
                            traceId, user.Id, user.Username);

                        await _authService.SignInAsync(HttpContext, user);
                        _logger.LogInformation("[TraceId:{TraceId}] Established local auth cookie session for user.", traceId);
                    }
                    else
                    {
                        _logger.LogWarning("[TraceId:{TraceId}] GetSSOUserInfoAsync returned null UserInfo.", traceId);
                    }
                }
                else
                {
                    _logger.LogWarning("[TraceId:{TraceId}] Access token is missing or empty in HttpContext.", traceId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId:{TraceId}] Failed to process SSO signin-complete workflow. Error: {Message}",
                    traceId, ex.Message);
            }

            _logger.LogInformation("[TraceId:{TraceId}] Redirecting to Home Index after signin-complete.", traceId);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet("/Account/Logout")]
        [HttpGet("/Logout")]
        public IActionResult Logout()
        {
            var traceId = HttpContext.TraceIdentifier;
            _logger.LogInformation("[TraceId:{TraceId}] Initiating SSO Logout.", traceId);

            return SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                "Cookies",
                "oidc");
        }
    }
}
