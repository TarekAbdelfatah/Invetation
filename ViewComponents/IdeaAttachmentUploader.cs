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

        /// <summary>
        /// Renders the attachment uploader for an existing idea or a draft.
        /// Pass either <c>ideaId</c> (the stable InnovationIdea.Id or the draft id)
        /// or <c>referenceNumber</c>. For drafts the idea does not yet exist in
        /// the database; the widget still renders and uploads go through the
        /// draft-specific endpoints, then move to the idea on submission.
        /// </summary>
        public async Task<IViewComponentResult> InvokeAsync(string? referenceNumber = null, Guid? ideaId = null)
        {
            Guid resolvedId = Guid.Empty;
            string resolvedRef = string.Empty;
            string resolvedTitle = string.Empty;
            bool existsInDb = false;

            if (ideaId is { } iid && iid != Guid.Empty)
            {
                var byId = await _db.InnovationIdeas
                    .AsNoTracking()
                    .Where(i => i.Id == iid)
                    .Select(i => new { i.Id, i.Title, i.ReferenceNumber })
                    .FirstOrDefaultAsync();
                if (byId is not null)
                {
                    resolvedId = byId.Id;
                    resolvedRef = byId.ReferenceNumber ?? string.Empty;
                    resolvedTitle = byId.Title;
                    existsInDb = true;
                }
                else
                {
                    resolvedId = iid;
                }
            }
            else if (!string.IsNullOrWhiteSpace(referenceNumber))
            {
                var byRef = await _db.InnovationIdeas
                    .AsNoTracking()
                    .Where(i => i.ReferenceNumber == referenceNumber)
                    .Select(i => new { i.Id, i.Title, i.ReferenceNumber })
                    .FirstOrDefaultAsync();
                if (byRef is null) return Content(string.Empty);
                resolvedId = byRef.Id;
                resolvedRef = byRef.ReferenceNumber ?? string.Empty;
                resolvedTitle = byRef.Title;
                existsInDb = true;
            }
            else
            {
                return Content(string.Empty);
            }

            ViewBag.ExistsInDb = existsInDb;
            return View("Default", new
            {
                Id = resolvedId,
                ReferenceNumber = resolvedRef,
                Title = resolvedTitle
            });
        }
    }
}
