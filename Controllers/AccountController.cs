using System.Security.Claims;
using Ibtikar.DTOs.Account;
using Ibtikar.Models;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Implementations;
using Ibtikar.ViewModels;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVm vm)
        {
            if (string.IsNullOrWhiteSpace(vm.Username) || string.IsNullOrWhiteSpace(vm.Password))
            {
                ModelState.AddModelError(string.Empty, "يرجى أدخال اسم المستخدم وكلمة المرور.");
                await PopulateDemoUsersAsync(HttpContext.RequestAborted);
                return View(vm);
            }

            var result = await _auth.LoginAsync(vm.Username, vm.Password, HttpContext.RequestAborted);
            if (!result.IsSuccess || result.User == null)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "بيانات الدخول غير صحيحة.");
                await PopulateDemoUsersAsync(HttpContext.RequestAborted);
                return View(vm);
            }

            await _auth.SignInAsync(HttpContext, result.User);

            if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            {
                return Redirect(vm.ReturnUrl);
            }

            return RedirectToAction("Index", "MyRequests");
        }

        [HttpPost]
        [HttpGet]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("Processing user logout request.");
            await _auth.SignOutAsync(HttpContext);

            try
            {
                HttpContext.Session?.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not clear session during logout.");
            }

            Response.Cookies.Delete(".Ibtikar.Auth");
            Response.Cookies.Delete("pkce_verifier");

            var postLogoutRedirectUri = $"{Request.Scheme}://{Request.Host}/Account/Login";
            var logoutUrl = _ssoService.BuildLogoutUrl(postLogoutRedirectUri);

            _logger.LogInformation("Local cookie cleared. Redirecting to IdentityServer end session endpoint: {LogoutUrl}", logoutUrl);
            return Redirect(logoutUrl);
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}