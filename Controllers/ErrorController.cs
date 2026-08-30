using Microsoft.AspNetCore.Mvc;

namespace Ibtikar.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{code:int}")]
        public IActionResult Status(int code)
        {
            Response.StatusCode = code;
            return code == 404 ? View("NotFound") : View("ServerError");
        }

        [Route("Error")]
        public IActionResult Index()
        {
            Response.StatusCode = 500;
            return View("ServerError");
        }
    }
}