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
                if (!string.IsNullOrEmpty(home)) return Redirect(home);
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

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Login(LoginVm vm)
        //{
        //    if (string.IsNullOrWhiteSpace(vm.Username) || string.IsNullOrWhiteSpace(vm.Password))
        //    {
        //        ModelState.AddModelError(string.Empty, "يرجى أدخال اسم المستخدم وكلمة المرور.");
        //        await PopulateDemoUsersAsync(HttpContext.RequestAborted);
        //        return View(vm);
        //    }

        //    var result = await _auth.LoginAsync(vm.Username, vm.Password, HttpContext.RequestAborted);
        //    if (!result.IsSuccess || result.User == null)
        //    {
        //        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "بيانات الدخول غير صحيحة.");
        //        await PopulateDemoUsersAsync(HttpContext.RequestAborted);
        //        return View(vm);
        //    }

        //    await _auth.SignInAsync(HttpContext, result.User);

        //    if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
        //    {
        //        return Redirect(vm.ReturnUrl);
        //    }

        //    return RedirectToAction("Index", "MyRequests");
        //}

        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("Processing user logout request.");
            var idTokenHint = User.FindFirst("id_token")?.Value
                ?? await HttpContext.GetTokenAsync("id_token")
                ?? Request.Cookies["id_token"];

            var postLogoutRedirectUri = $"{Request.Scheme}://{Request.Host}/signout-callback-oidc";
            var logoutUrl = _ssoService.BuildLogoutUrl(postLogoutRedirectUri, idTokenHint);

            await ClearAllCookiesAndSessionAsync(HttpContext);

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
            await ClearAllCookiesAndSessionAsync(HttpContext);

            _logger.LogInformation("All cookies, session and tokens cleared. Redirecting to Login page.");
            return RedirectToAction("Index", "Home");
        }

        private async Task ClearAllCookiesAndSessionAsync(HttpContext context)
        {
            try
            {
                await _auth.SignOutAsync(context);
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignOutAsync failed during logout.");
            }

            try
            {
                context.Session?.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Session.Clear() failed during logout.");
            }

            try
            {
                foreach (var cookieKey in context.Request.Cookies.Keys)
                {
                    context.Response.Cookies.Delete(cookieKey);
                    context.Response.Cookies.Delete(cookieKey, new CookieOptions { Path = "/" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete request cookies during logout.");
            }

            var explicitCookies = new[] { ".Ibtikar.Auth", ".AspNetCore.Cookies", ".AspNetCore.Session", "pkce_verifier", "idsvr.session", "ARRAffinity", "ARRAffinitySameSite" };
            foreach (var name in explicitCookies)
            {
                try
                {
                    context.Response.Cookies.Delete(name);
                    context.Response.Cookies.Delete(name, new CookieOptions { Path = "/" });
                    context.Response.Cookies.Delete(name, new CookieOptions { Path = "/", Secure = true });
                    context.Response.Cookies.Delete(name, new CookieOptions { Path = "/", HttpOnly = true });
                }
                catch { }
            }
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}