using Ibtikar.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Controllers
{
    public class IdeasController : Controller
    {
        private readonly IbtikarDbContext _db;

        public IdeasController(IbtikarDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
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
    }
}