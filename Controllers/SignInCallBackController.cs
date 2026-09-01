using Ibtikar.Services.Implementations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Ibtikar.Controllers
{
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
            // After OIDC middleware processes the SSO callback at /signin-callback,
            // it will redirect the user to our SigninComplete action.
            var props = new AuthenticationProperties
            {
                RedirectUri = "/signin-complete"
            };

            return Challenge(props, "oidc");
        }

        /// <summary>
        /// Called after the OIDC middleware has processed the SSO callback (/signin-callback) and signed the user in.
        /// Fetches user info from SSO, saves or updates the user in DB, and establishes local cookie session.
        /// </summary>
        [HttpGet("/signin-complete")]
        public async Task<IActionResult> SigninComplete()
        {
            try
            {
                // Read the access_token saved by the OIDC middleware (SaveTokens = true)
                var accessToken = await HttpContext.GetTokenAsync("access_token");

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync SSO user info in SigninComplete.");
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet("/Account/Logout")]
        [HttpGet("/Logout")]
        public IActionResult Logout()
        {
            return SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                "Cookies",
                "oidc");
        }
    }
}
