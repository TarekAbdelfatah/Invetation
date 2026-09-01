using System.Diagnostics;
using Ibtikar.Services.Helpers;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Ibtikar.Controllers
{
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;
        private readonly IWebHostEnvironment _env;

        public ErrorController(ILogger<ErrorController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        [Route("Error/{code:int}")]
        public IActionResult Status(int code, string? traceId = null)
        {
            var (vm, status) = BuildForCode(code, traceId);
            Response.StatusCode = status;
            return View("Index", vm);
        }

        [Route("Error")]
        public IActionResult Index()
        {
            var (vm, status) = BuildForUnhandled();
            Response.StatusCode = status;
            return View("Index", vm);
        }

        [Route("Error/NotFound")]
        public IActionResult NotFoundPage()
        {
            var vm = new PublicErrorVm
            {
                Code = 404,
                Title = "الصفحة غير موجودة",
                Message = "الصفحة التي تبحث عنها قد تكون قد نُقلت أو حُذفت. تأكد من صحة الرابط أو ارجع لقائمة طلباتك.",
                Icon = "travel_explore",
                HomeHref = HomeForUser(),
                RequestId = HttpContext.TraceIdentifier,
                ShowException = _env.IsDevelopment()
            };
            Response.StatusCode = 404;
            return View("Index", vm);
        }

        private (PublicErrorVm Vm, int Status) BuildForCode(int code, string? traceId)
        {
            var id = string.IsNullOrWhiteSpace(traceId) ? HttpContext.TraceIdentifier : traceId;
            var vm = code switch
            {
                404 => new PublicErrorVm
                {
                    Code = 404,
                    Title = "الصفحة غير موجودة",
                    Message = "الصفحة التي تبحث عنها قد تكون قد نُقلت أو حُذفت. تأكد من صحة الرابط أو ارجع لقائمة طلباتك.",
                    Icon = "travel_explore",
                    HomeHref = HomeForUser(),
                    RequestId = id,
                    ShowException = _env.IsDevelopment()
                },
                403 => new PublicErrorVm
                {
                    Code = 403,
                    Title = "غير مصرح بالدخول",
                    Message = "ليست لديك صلاحية الوصول إلى هذه الصفحة. إذا كنت تعتقد أن هذا خطأ، يرجى مراجعة مدير النظام.",
                    Icon = "lock",
                    HomeHref = HomeForUser(),
                    RequestId = id,
                    ShowException = _env.IsDevelopment()
                },
                401 => new PublicErrorVm
                {
                    Code = 401,
                    Title = "يلزم تسجيل الدخول",
                    Message = "يجب تسجيل الدخول أولاً للوصول إلى هذه الصفحة.",
                    Icon = "login",
                    HomeHref = "/Account/Login",
                    RequestId = id,
                    ShowException = _env.IsDevelopment()
                },
                _ => new PublicErrorVm
                {
                    Code = code,
                    Title = "حدث خطأ",
                    Message = "حدث خطأ غير متوقع. تم تسجيل الحادثة وسيتم معالجتها قريباً.",
                    Icon = "error_outline",
                    HomeHref = HomeForUser(),
                    RequestId = id,
                    ShowException = _env.IsDevelopment()
                }
            };
            return (vm, code);
        }

        private (PublicErrorVm Vm, int Status) BuildForUnhandled()
        {
            var id = HttpContext.TraceIdentifier;
            var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
            Exception? ex = context?.Error;
            while (ex?.InnerException is not null) ex = ex.InnerException;

            if (ex is not null)
            {
                _logger.LogError(ex, "Unhandled exception. TraceId={TraceId}", id);
            }

            return (new PublicErrorVm
            {
                Code = 500,
                Title = "خطأ غير متوقع",
                Message = "حدث خطأ أثناء معالجة طلبك. تم تسجيل الحادثة وسيتم معالجتها من قبل فريق الدعم.",
                Icon = "error_outline",
                HomeHref = HomeForUser(),
                RequestId = id,
                ShowException = _env.IsDevelopment(),
                ExceptionMessage = _env.IsDevelopment() ? ex?.Message : null
            }, 500);
        }

        private string HomeForUser()
        {
            if (User.Identity?.IsAuthenticated != true) return "/Account/Login";
            foreach (var role in RoleCodes.HomeRedirects)
            {
                if (User.IsInRole(role.Key)) return role.Value;
            }
            return "/";
        }
    }
}
