using System.Security.Claims;
using Ibtikar.Services.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var home = RoleRedirect.ResolveHomeFor(User);
                if (!string.IsNullOrEmpty(home)) return Redirect(home);
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewData["Error"] = "أدخل اسم المستخدم وكلمة المرور.";
                return View();
            }

            try
            {
                var result = await _auth.LoginAsync(username, password, HttpContext.RequestAborted);
                if (!result.IsSuccess || result.User is null)
                {
                    _logger.LogWarning("Login failed for {Username}", username);
                    ViewData["Error"] = result.ErrorMessage;
                    return View();
                }

                await _auth.SignInAsync(HttpContext, result.User, HttpContext.RequestAborted);

                var home = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                    ? returnUrl
                    : (RoleRedirect.ResolveHomeFor(result.User.UserRoles.Select(ur => ur.Role!.Code).ToList()) ?? "/Ideas");

                return Redirect(home);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                ViewData["Error"] = "حدث خطأ أثناء تسجيل الدخول. حاول مرة أخرى.";
                return View();
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

