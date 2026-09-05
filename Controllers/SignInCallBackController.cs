using Ibtikar.DTOs;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Implementations;
using Ibtikar.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ibtikar.Controllers
{
    public class SignInCallBackController : Controller
    {
        private readonly ILogger<SignInCallBackController> _logger;
        private readonly SsoService _ssoService;
        private readonly IAuthService _authService;

        public SignInCallBackController(
            ILogger<SignInCallBackController> logger,
            SsoService ssoService,
            IAuthService authService)
        {
            _logger = logger;
            _ssoService = ssoService;
            _authService = authService;
        }

        [HttpGet("/Account/Login")]
        [HttpGet("/Login")]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var home = RoleRedirect.ResolveHomeFor(User);
                if (home != null) return RedirectToAction(home.Action, home.Controller);
                return RedirectToAction(RoleCodes.DefaultBeneficiaryHome.Action, RoleCodes.DefaultBeneficiaryHome.Controller);
            }

            // After OIDC middleware processes the SSO callback at /signin-callback,
            // it will redirect the user to our SigninComplete action.
            var props = new AuthenticationProperties
            {
                RedirectUri = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                    ? returnUrl
                    : (Url.Action("SigninComplete") ?? (Request.PathBase + "/signin-complete"))
            };

            return Challenge(props, "oidc");
        }

        /// <summary>
        /// Called after the OIDC middleware has processed the SSO callback (/signin-callback) and signed the user in.
        /// Fetches user info from SSO, saves or updates the user in DB, then redirects to the appropriate page.
        /// </summary>
        [HttpGet("/signin-complete")]
        [AllowAnonymous]
        public async Task<IActionResult> SigninComplete(string? returnUrl = null)
        {
            string roleCode = string.Empty;
            try
            {
                // Read the access_token saved by the OIDC middleware (SaveTokens = true)
                var accessToken = await HttpContext.GetTokenAsync("access_token");

                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    var userInfo = await _ssoService.GetSSOUserInfoAsync(accessToken);
                    if (userInfo != null)
                    {
                        var (user, resolvedRole) = await _authService.SyncSsoUserAsync(userInfo);
                        roleCode = resolvedRole;
                        var idToken = await HttpContext.GetTokenAsync("id_token");
                        await _authService.SignInAsync(HttpContext, user, roleCode, idToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync SSO user info in SigninComplete.");
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (!string.IsNullOrEmpty(roleCode) && RoleCodes.HomeRedirects.TryGetValue(roleCode, out var route))
            {
                return RedirectToAction(route.Action, route.Controller);
            }

            return RedirectToAction(RoleCodes.DefaultBeneficiaryHome.Action, RoleCodes.DefaultBeneficiaryHome.Controller);
        }

        [AcceptVerbs("GET", "POST")]
        [Route("/Account/Logout")]
        [Route("/Logout")]
        [Route("/SSO/Logout")]
        public async Task<IActionResult> Logout()
        {
            await _authService.SignOutAsync(HttpContext);
            await AuthCookieClearer.ClearAsync(HttpContext, _logger);

            return SignOut(
                new AuthenticationProperties { RedirectUri = Request.PathBase + "/" },
                "Cookies",
                "oidc");
        }

        [HttpGet("/signout-callback-oidc")]
        [HttpGet("/signout-callback")]
        [HttpGet("/Account/SignOutCallback")]
        [AllowAnonymous]
        public async Task<IActionResult> SignOutCallback()
        {
            await _authService.SignOutAsync(HttpContext);
            await AuthCookieClearer.ClearAsync(HttpContext, _logger);

            return RedirectToAction("Index", "Home");
        }
    }
}
