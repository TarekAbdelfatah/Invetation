using Ibtikar.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.ViewComponents
{
    public class IdeaAttachmentUploader : ViewComponent
    {
        private readonly IbtikarDbContext _db;

        public IdeaAttachmentUploader(IbtikarDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync(string referenceNumber)
        {
            if (string.IsNullOrWhiteSpace(referenceNumber))
                return Content(string.Empty);

            var idea = await _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => i.ReferenceNumber == referenceNumber)
                .Select(i => new { i.Id, i.Title, i.ReferenceNumber })
                .FirstOrDefaultAsync();

            if (idea is null)
                return Content(string.Empty);

            return View("Default", idea);
        }
    }
}
