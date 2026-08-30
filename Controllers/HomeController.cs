using System.Security.Claims;
using Ibtikar.Services.Security;
using Microsoft.AspNetCore.Mvc;

namespace Ibtikar.Controllers
{
    public class HomeController : Controller
    {
        private readonly Ibtikar.Data.IbtikarDbContext _db;
        private readonly ILogger<HomeController> _logger;

        public HomeController(Ibtikar.Data.IbtikarDbContext db, ILogger<HomeController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var codes = User.FindAll(RoleCodes.ClaimType).Select(c => c.Value).ToList();
                foreach (var code in codes)
                {
                    if (RoleCodes.HomeRedirects.TryGetValue(code, out var path))
                        return Redirect(path);
                }
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new Models.ErrorViewModel
            {
                RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
