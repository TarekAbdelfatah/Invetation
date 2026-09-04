using System.Security.Claims;
using Ibtikar.DTOs.Account;
using Ibtikar.Models;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Implementations;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ibtikar.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _auth;
        private readonly SsoService _ssoService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            AuthService auth,
            SsoService ssoService,
            ILogger<AccountController> logger)
        {
            _auth = auth;
            _ssoService = ssoService;
            _logger = logger;
        }

        public readonly record struct DemoUser(string Username, string FullName, string RoleName);

        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var home = RoleRedirect.ResolveHomeFor(User);
                if (home != null) return RedirectToAction(home.Action, home.Controller);
            }

            var model = new LoginVm { ReturnUrl = returnUrl };
            ViewData["ReturnUrl"] = returnUrl;
            await PopulateDemoUsersAsync(HttpContext.RequestAborted);
            return View(model);
        }

        private async Task PopulateDemoUsersAsync(CancellationToken ct)
        {
            if (ViewData.ContainsKey("DemoUsers")) return;
            var demoUsers = await _auth.GetDemoUsersAsync(ct);
            ViewData["DemoUsers"] = demoUsers
                .Select(d => new DemoUser(d.Username, d.FullName, "مستخدم"))
                .ToList();
        }


        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("Processing user logout request.");
            var idTokenHint = User.FindFirst("id_token")?.Value
                ?? await HttpContext.GetTokenAsync("id_token")
                ?? Request.Cookies["id_token"];

            var postLogoutRedirectUri = _ssoService.GetPostLogoutRedirectUri() 
                ?? $"https://{Request.Host}{Request.PathBase}/signout-callback-oidc";
            var logoutUrl = _ssoService.BuildLogoutUrl(postLogoutRedirectUri, idTokenHint);

            await _auth.SignOutAsync(HttpContext);
            await AuthCookieClearer.ClearAsync(HttpContext, _logger);

            _logger.LogInformation("Local cookies & sessions cleared. Redirecting to IdentityServer end session endpoint: {LogoutUrl}", logoutUrl);
            return Redirect(logoutUrl);
        }

        [HttpGet("/signout-callback-oidc")]
        [HttpGet("/signout-callback")]
        [HttpGet("Account/SignOutCallback")]
        [AllowAnonymous]
        public async Task<IActionResult> SignOutCallback()
        {
            _logger.LogInformation("SignOut callback received from IdentityServer.");
            await _auth.SignOutAsync(HttpContext);
            await AuthCookieClearer.ClearAsync(HttpContext, _logger);

            _logger.LogInformation("All cookies, session and tokens cleared. Redirecting to Login page.");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}