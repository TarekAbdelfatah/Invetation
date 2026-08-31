using Ibtikar.Services.Security;
using Microsoft.AspNetCore.Mvc;

namespace Ibtikar.Controllers
{
    public class HomeController : Controller
    {
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
            return View(new Models.ErrorViewModel
            {
                RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}