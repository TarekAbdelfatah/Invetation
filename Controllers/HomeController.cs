using Ibtikar.Options;
using Ibtikar.Services.Helpers;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Ibtikar.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<HomeController> _logger;
        private readonly ErrorOptions _errorOptions;

        public HomeController(
            IWebHostEnvironment env,
            ILogger<HomeController> logger,
            IOptions<ErrorOptions> errorOptions)
        {
            _env = env;
            _logger = logger;
            _errorOptions = errorOptions.Value;
        }

        public IActionResult Index()
        {
            if (User.Identity is { IsAuthenticated: true })
            {
                var home = RoleRedirect.ResolveHomeFor(User);
                if (home != null) return RedirectToAction(home.Action, home.Controller);
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet("/AccessDenied")]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var status = HttpContext.Response.StatusCode;
            if (status < 400) status = 500;
            var traceId = HttpContext.TraceIdentifier;
            var home = ResolveHome();

            var show = _env.IsDevelopment() || _errorOptions.ShowDetailsInProduction;
            var showStack = _env.IsDevelopment() || _errorOptions.ShowStackTraceInProduction;
            string? exType = null, exMsg = null, exStack = null;
            if (show)
            {
                var ctx = HttpContext.Features.Get<IExceptionHandlerFeature>();
                Exception? ex = ctx?.Error;
                while (ex?.InnerException is not null) ex = ex.InnerException;
                exType = ex?.GetType().FullName;
                exMsg = ex?.Message;
                exStack = showStack ? ex?.StackTrace : null;
            }

            var vm = new PublicErrorVm
            {
                Code = status,
                Title = status switch
                {
                    404 => "الصفحة غير موجودة",
                    403 => "غير مصرح بالدخول",
                    401 => "يلزم تسجيل الدخول",
                    _ => "خطأ غير متوقع"
                },
                Message = status switch
                {
                    404 => "الصفحة التي تبحث عنها قد تكون قد نُقلت أو حُذفت. تأكد من صحة الرابط أو ارجع لقائمة طلباتك.",
                    403 => "ليست لديك صلاحية الوصول إلى هذه الصفحة. إذا كنت تعتقد أن هذا خطأ، يرجى مراجعة مدير النظام.",
                    401 => "يجب تسجيل الدخول أولاً للوصول إلى هذه الصفحة.",
                    _ => "حدث خطأ أثناء معالجة طلبك. تم تسجيل الحادثة وسيتم معالجتها من قبل فريق الدعم."
                },
                Icon = status == 404 ? "travel_explore" : status == 403 ? "lock" : "error_outline",
                HomeHref = home,
                RequestId = traceId,
                ShowException = show,
                ExceptionType = exType,
                ExceptionMessage = exMsg,
                ExceptionStackTrace = exStack
            };

            _logger.LogWarning("Home/Error rendered. Status={Status} TraceId={TraceId} Type={Type}", status, traceId, exType);
            return View(vm);
        }

        private string ResolveHome()
        {
            if (User.Identity?.IsAuthenticated != true) return Url.Action("Login", "Account") ?? "/Account/Login";
            foreach (var role in RoleCodes.HomeRedirects)
            {
                if (User.IsInRole(role.Key)) return Url.Action(role.Value.Action, role.Value.Controller) ?? "/";
            }
            return "/";
        }
    }
}