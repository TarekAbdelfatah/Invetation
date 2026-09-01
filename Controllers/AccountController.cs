using System.Security.Claims;
using Ibtikar.Data;
using Ibtikar.Models;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Implementations;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _auth;
        private readonly IbtikarDbContext _db;
        private readonly ILogger<AccountController> _logger;

        public AccountController(AuthService auth, IbtikarDbContext db, ILogger<AccountController> logger)
        {
            _auth = auth;
            _db = db;
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
            PopulateDemoUsers();
            return View(model);
        }

        private void PopulateDemoUsers()
        {
            if (ViewData.ContainsKey("DemoUsers")) return;
            var demoUsers = _db.Users
                .AsNoTracking()
                .Where(u => u.IsActive)
                .SelectMany(u => u.UserRoles, (u, ur) => new { u.Username, u.FullName, RoleName = ur.Role!.Name })
                .OrderBy(x => x.Username)
                .Select(x => new DemoUser(x.Username, x.FullName, x.RoleName))
                .ToList();
            ViewData["DemoUsers"] = demoUsers;
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
                PopulateDemoUsers();
                return View(viewVm);
            }

            try
            {
                var result = await _auth.LoginAsync(vm.Username, vm.Password, HttpContext.RequestAborted);
                if (!result.IsSuccess || result.User is null)
                {
                    _logger.LogWarning("Login failed for {Username}", vm.Username);
                    ViewData["Error"] = result.ErrorMessage;
                    PopulateDemoUsers();
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
                PopulateDemoUsers();
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

