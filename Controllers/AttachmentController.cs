using Ibtikar.Data;
using Ibtikar.Models;
using Ibtikar.Services.Implementations;
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
        private readonly AttachmentService _attachments;
        private readonly FileStorageService _storage;
        private readonly IbtikarDbContext _db;
        private readonly ILogger<AttachmentController> _logger;

        public AttachmentController(
            AttachmentService attachments,
            FileStorageService storage,
            IbtikarDbContext db,
            ILogger<AttachmentController> logger)
        {
            _attachments = attachments;
            _storage = storage;
            _db = db;
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

        [HttpPost("uploadDraft")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> UploadDraft(
            [FromForm] Guid draftId,
            [FromForm] IFormFile file,
            CancellationToken ct)
        {
            if (draftId == Guid.Empty)
                return BadRequest(new { error = "draftId is required." });
            if (file is null || file.Length == 0)
                return BadRequest(new { error = "file is required." });

            var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdRaw, out var userId))
                return Challenge();

            var result = _attachments.SaveDraftAsync(userId, draftId, file, ct);
            if (!result.Success)
                return UnprocessableEntity(new { error = result.Error });

            var existingCount = _attachments.CountDraftAsync(userId, draftId);
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

        [HttpGet("listDraft")]
        public IActionResult ListDraft(
            [FromQuery] Guid draftId,
            CancellationToken ct)
        {
            if (draftId == Guid.Empty) return BadRequest(new { error = "draftId required." });

            var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdRaw, out var userId))
                return Challenge();

            var items = _attachments.ListDraftAsync(userId, draftId);
            return Ok(new
            {
                maxBytes = _attachments.MaxBytes,
                maxCount = _attachments.MaxCount,
                items = items.Select(a => new
                {
                    a.Id,
                    a.FileName,
                    a.SizeBytes,
                    a.UploadedAt
                })
            });
        }

        [HttpPost("deleteDraft")]
        public IActionResult DeleteDraft(
            [FromForm] Guid draftId,
            [FromForm] string fileName,
            CancellationToken ct)
        {
            if (draftId == Guid.Empty) return BadRequest(new { error = "draftId required." });
            if (string.IsNullOrWhiteSpace(fileName)) return BadRequest(new { error = "fileName required." });

            var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdRaw, out var userId))
                return Challenge();

            var ok = _attachments.DeleteDraftFile(userId, draftId, fileName);
            return ok ? Ok(new { ok = true }) : NotFound();
        }

        [HttpGet("download/{attachmentId:guid}")]
        public async Task<IActionResult> Download(Guid attachmentId, CancellationToken ct)
        {
            var attachment = await _db.IdeaAttachments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);
            if (attachment is null) return NotFound();

            if (!await _attachments.UserOwnsIdeaAsync(attachment.InnovationIdeaId, ResolveUserId(), ct))
                return Forbid();

            if (!System.IO.File.Exists(attachment.StoragePath))
                return NotFound(new { error = "file not found on disk." });

            var stream = System.IO.File.OpenRead(attachment.StoragePath);
            return File(stream, attachment.ContentType ?? "application/pdf", attachment.FileName);
        }

        [HttpGet("downloadDraft")]
        public IActionResult DownloadDraft(
            [FromQuery] Guid draftId,
            [FromQuery] string fileName,
            CancellationToken ct)
        {
            if (draftId == Guid.Empty) return BadRequest(new { error = "draftId required." });
            if (string.IsNullOrWhiteSpace(fileName)) return BadRequest(new { error = "fileName required." });

            var userId = ResolveUserId();
            var folder = Path.Combine(_storage.Root, "_drafts", userId.ToString("N"), draftId.ToString("N"));
            var path = Path.Combine(folder, fileName);
            if (!System.IO.File.Exists(path)) return NotFound();
            var stream = System.IO.File.OpenRead(path);
            return File(stream, "application/pdf", fileName);
        }

        private Guid ResolveUserId()
        {
            var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
        }
    }
}
