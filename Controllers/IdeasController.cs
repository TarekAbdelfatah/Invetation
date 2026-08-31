using Ibtikar.Data;
using Ibtikar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Controllers
{
    public class IdeasController : Controller
    {
        private readonly IbtikarDbContext _db;
        private readonly ILogger<IdeasController> _logger;

        public IdeasController(IbtikarDbContext db, ILogger<IdeasController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            try
            {
                var ideas = await _db.InnovationIdeas
                    .AsNoTracking()
                    .Include(i => i.CurrentStatus)
                    .Include(i => i.InnovationDomain)
                    .Include(i => i.ApplicantDepartment)
                    .OrderByDescending(i => i.CreatedAt)
                    .Take(50)
                    .ToListAsync();

                return View(ideas);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ideas index fallback (database unavailable): {Message}", ex.Message);
                ViewBag.DatabaseError = ex.Message;
                return View(Array.Empty<InnovationIdea>());
            }
        }

        [Authorize]
        public IActionResult Create()
        {
            return View();
        }
    }
}
