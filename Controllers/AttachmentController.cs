using Ibtikar.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ibtikar.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class AttachmentController : ControllerBase
    {
        private readonly AttachmentService _attachments;
        private readonly ILogger<AttachmentController> _logger;

        public AttachmentController(
            AttachmentService attachments,
            ILogger<AttachmentController> logger)
        {
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

            if (!await _attachments.UserOwnsIdeaAsync(ideaId, userId, ct))
                return Forbid();

            var result = await _attachments.SaveAsync(ideaId, userId, file, ct);
            if (!result.Success)
                return UnprocessableEntity(new { error = result.Error });

            var existingCount = await _attachments.CountForIdeaAsync(ideaId, ct);
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

            if (!await _attachments.UserOwnsIdeaAsync(ideaId, userId, ct))
                return Forbid();

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