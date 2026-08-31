using Ibtikar.Data;
using Ibtikar.Models;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        public async Task<IActionResult> Create()
        {
            var model = new IdeaCreateViewModel();
            await PopulateLookupsAsync(model);
            return View(model);
        }

        private async Task PopulateLookupsAsync(IdeaCreateViewModel model)
        {
            ViewBag.InnovationDomains = await _db.InnovationDomains
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync();

            ViewBag.ExpectedImpacts = await _db.ExpectedImpacts
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync();

            ViewBag.TargetAudiences = await _db.TargetAudiences
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync();

            ViewBag.Technologies = await _db.Technologies
                .Where(t => t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync();
        }
    }
}
