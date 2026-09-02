using Ibtikar.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Ibtikar.ViewComponents
{
    public class IdeaAttachmentUploader : ViewComponent
    {
        private readonly IIdeaService _ideas;

        public IdeaAttachmentUploader(IIdeaService ideas)
        {
            _ideas = ideas;
        }

        /// <summary>
        /// Renders the attachment uploader for an existing idea or a draft.
        /// Pass either <c>ideaId</c> (the stable InnovationIdea.Id or the draft id)
        /// or <c>referenceNumber</c>. For drafts the idea does not yet exist in
        /// the database; the widget still renders and uploads go through the
        /// draft-specific endpoints, then move to the idea on submission.
        /// </summary>
        public async Task<IViewComponentResult> InvokeAsync(string? referenceNumber = null, Guid? ideaId = null, bool readOnly = false)
        {
            Guid resolvedId = Guid.Empty;
            string resolvedRef = string.Empty;
            string resolvedTitle = string.Empty;
            bool existsInDb = false;

            if (ideaId is { } iid && iid != Guid.Empty)
            {
                var meta = await _ideas.GetMetaAsync(ideaId: iid, referenceNumber: null, HttpContext.RequestAborted);
                if (meta is not null)
                {
                    resolvedId = meta.Id;
                    resolvedRef = meta.ReferenceNumber ?? string.Empty;
                    resolvedTitle = meta.Title;
                    existsInDb = true;
                }
                else
                {
                    resolvedId = iid;
                }
            }
            else if (!string.IsNullOrWhiteSpace(referenceNumber))
            {
                var meta = await _ideas.GetMetaAsync(ideaId: null, referenceNumber: referenceNumber, HttpContext.RequestAborted);
                if (meta is null) return Content(string.Empty);
                resolvedId = meta.Id;
                resolvedRef = meta.ReferenceNumber ?? string.Empty;
                resolvedTitle = meta.Title;
                existsInDb = true;
            }
            else
            {
                return Content(string.Empty);
            }

            ViewBag.ExistsInDb = existsInDb;
            ViewBag.ReadOnly = readOnly;
            return View("Default", new
            {
                Id = resolvedId,
                ReferenceNumber = resolvedRef,
                Title = resolvedTitle,
                ReadOnly = readOnly
            });
        }
    }
}