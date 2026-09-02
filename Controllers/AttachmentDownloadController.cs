using Ibtikar.Services.Helpers;
using Ibtikar.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ibtikar.Controllers
{
    [Authorize]
    public class AttachmentDownloadController : Controller
    {
        private readonly AttachmentService _attachments;
        private readonly FileStorageService _storage;

        public AttachmentDownloadController(AttachmentService attachments, FileStorageService storage)
        {
            _attachments = attachments;
            _storage = storage;
        }

        [HttpGet]
        public async Task<IActionResult> Download(Guid attachmentId, CancellationToken ct)
        {
            var attachment = await _attachments.GetByIdAsync(attachmentId, ct);
            if (attachment is null) return NotFound();

            var current = ResolveCurrentUser();
            if (current is null
                || !await _attachments.CanAccessIdeaAsync(
                    attachment.InnovationIdeaId,
                    current.Value.UserId,
                    current.Value.RoleCodes,
                    current.Value.DepartmentId,
                    ct))
                return Forbid();

            if (!System.IO.File.Exists(attachment.StoragePath))
                return NotFound();

            var fullStoragePath = Path.GetFullPath(attachment.StoragePath);
            if (!fullStoragePath.StartsWith(Path.GetFullPath(_storage.Root), StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var stream = System.IO.File.OpenRead(attachment.StoragePath);
            return File(stream, "application/pdf", attachment.FileName);
        }

        private (Guid UserId, IReadOnlyCollection<string> RoleCodes, Guid? DepartmentId)? ResolveCurrentUser()
        {
            var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdRaw, out var userId)) return null;

            var roles = User.FindAll(RoleCodes.ClaimType)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            Guid? departmentId = null;
            var deptRaw = User.FindFirst(RoleCodes.DepartmentIdClaim)?.Value;
            if (!string.IsNullOrEmpty(deptRaw) && Guid.TryParse(deptRaw, out var deptId))
                departmentId = deptId;

            return (userId, roles, departmentId);
        }
    }
}