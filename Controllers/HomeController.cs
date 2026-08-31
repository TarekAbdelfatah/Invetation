using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            var home = Ibtikar.Services.Security.RoleRedirect.ResolveHomeFor(User);
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
            return View(new Models.ErrorViewModel
            {
                RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
