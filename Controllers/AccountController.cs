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
        private readonly ILogger<AccountController> _logger;

        public AccountController(AuthService auth, ILogger<AccountController> logger)
        {
            _auth = auth;
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
                .Select(d => new DemoUser(d.Username, d.FullName, d.RoleName))
                .ToList();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVm vm)
        {
            ViewData["ReturnUrl"] = vm.ReturnUrl;
            if (!ModelState.IsValid)
            {
                var viewVm = new LoginVm
                {
                    Username = vm.Username,
                    Password = vm.Password,
                    ReturnUrl = vm.ReturnUrl
                };
                await PopulateDemoUsersAsync(HttpContext.RequestAborted);
                return View(viewVm);
            }

            try
            {
                var result = await _auth.LoginAsync(vm.Username, vm.Password, HttpContext.RequestAborted);
                if (!result.IsSuccess || result.User is null)
                {
                    _logger.LogWarning("Login failed for {Username}", vm.Username);
                    ViewData["Error"] = result.ErrorMessage;
                    await PopulateDemoUsersAsync(HttpContext.RequestAborted);
                    return View(vm);
                }

                await _auth.SignInAsync(HttpContext, result.User, HttpContext.RequestAborted);

                var returnUrl = vm.ReturnUrl;
                var home = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                    ? returnUrl
                    : (RoleRedirect.ResolveHomeFor(result.User.UserRoles.Select(ur => ur.Role!.Code).ToList()) ?? "/Ideas");

                return Redirect(home);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                ViewData["Error"] = "حدث خطأ أثناء تسجيل الدخول. حاول مرة أخرى.";
                await PopulateDemoUsersAsync(HttpContext.RequestAborted);
                return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _auth.SignOutAsync(HttpContext);
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}