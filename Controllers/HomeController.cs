using Ibtikar.Services.Helpers;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Ibtikar.Controllers
{
    public class HomeController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IWebHostEnvironment env, ILogger<HomeController> logger)
        {
            _env = env;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var home = RoleRedirect.ResolveHomeFor(User);
            if (!string.IsNullOrEmpty(home)) return Redirect(home);
            return View();
        }

        public IActionResult Privacy()
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
                ShowException = _env.IsDevelopment()
            };

            _logger.LogWarning("Home/Error rendered. Status={Status} TraceId={TraceId}", status, traceId);
            return View(vm);
        }

        private string ResolveHome()
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