using Ibtikar.Data;
using Ibtikar.Services.Attachments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Ibtikar.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class AttachmentController : ControllerBase
    {
        private readonly IbtikarDbContext _db;
        private readonly AttachmentService _attachments;
        private readonly ILogger<AttachmentController> _logger;

        public AttachmentController(
            IbtikarDbContext db,
            AttachmentService attachments,
            ILogger<AttachmentController> logger)
        {
            _db = db;
            _attachments = attachments;
            _logger = logger;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> Upload(
            [FromForm] Guid ideaId,
            [FromForm] IFormFile file,
            CancellationToken ct)
        {
            if (ideaId == Guid.Empty)
                return BadRequest(new { error = "ideaId is required." });
            if (file is null || file.Length == 0)
                return BadRequest(new { error = "file is required." });

            var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdRaw, out var userId))
                return Challenge();

            var ownsIdea = await _db.InnovationIdeas
                .AsNoTracking()
                .AnyAsync(i => i.Id == ideaId && i.ApplicantUserId == userId, ct);
            if (!ownsIdea)
                return Forbid();

            var result = await _attachments.SaveAsync(ideaId, userId, file, ct);
            if (!result.Success)
                return UnprocessableEntity(new { error = result.Error });

            var existingCount = await _db.IdeaAttachments.CountAsync(a => a.InnovationIdeaId == ideaId, ct);
            return Ok(new
            {
                id = result.AttachmentId,
                fileName = result.FileName,
                sizeBytes = result.SizeBytes,
                remaining = Math.Max(0, _attachments.MaxCount - existingCount),
                maxBytes = _attachments.MaxBytes,
                maxCount = _attachments.MaxCount
            });
        }

        [HttpGet("list")]
        public async Task<IActionResult> List(
            [FromQuery] Guid ideaId,
            CancellationToken ct)
        {
            if (ideaId == Guid.Empty) return BadRequest(new { error = "ideaId required." });

            var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdRaw, out var userId))
                return Challenge();

            var ownsIdea = await _db.InnovationIdeas
                .AsNoTracking()
                .AnyAsync(i => i.Id == ideaId && i.ApplicantUserId == userId, ct);
            if (!ownsIdea) return Forbid();

            var items = await _attachments.ListForIdeaAsync(ideaId, ct);
            return Ok(new
            {
                maxBytes = _attachments.MaxBytes,
                maxCount = _attachments.MaxCount,
                items = items.Select(a => new
                {
                    a.Id,
                    a.FileName,
                    a.ContentType,
                    a.SizeBytes,
                    a.UploadedAt
                })
            });
        }
    }
}
