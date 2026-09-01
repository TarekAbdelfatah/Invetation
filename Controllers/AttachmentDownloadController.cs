using Ibtikar.Data;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Ibtikar.Controllers
{
    [Authorize]
    public class AttachmentDownloadController : Controller
    {
        private readonly IbtikarDbContext _db;
        private readonly AttachmentService _attachments;
        private readonly FileStorageService _storage;

        public AttachmentDownloadController(
            IbtikarDbContext db,
            AttachmentService attachments,
            FileStorageService storage)
        {
            _db = db;
            _attachments = attachments;
            _storage = storage;
        }

        [HttpGet]
        public async Task<IActionResult> Download(Guid attachmentId, CancellationToken ct)
        {
            var attachment = await _db.IdeaAttachments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);
            if (attachment is null) return NotFound();

            if (!await CanAccessAsync(attachment.InnovationIdeaId, ct))
                return Forbid();

            if (!System.IO.File.Exists(attachment.StoragePath))
                return NotFound();

            var stream = System.IO.File.OpenRead(attachment.StoragePath);
            return File(stream, attachment.ContentType ?? "application/pdf", attachment.FileName);
        }

        private Guid? CurrentUserId =>
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : (Guid?)null;

        private async Task<bool> CanAccessAsync(Guid ideaId, CancellationToken ct)
        {
            var userId = CurrentUserId;
            if (userId is null) return false;

            // Owner
            if (await _attachments.UserOwnsIdeaAsync(ideaId, userId.Value, ct))
                return true;

            // Audit / Specialized / Partner / Committee / Admin roles: any of them can access.
            var roleClaims = User.FindAll(RoleCodes.ClaimType).Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (roleClaims.Contains(RoleCodes.SystemAdmin)
                || roleClaims.Contains(RoleCodes.AuditEmployee)
                || roleClaims.Contains(RoleCodes.SpecializedDepartment)
                || roleClaims.Contains(RoleCodes.PartnerDepartment)
                || roleClaims.Contains(RoleCodes.InnovationCommitteeMember))
            {
                return true;
            }

            // Assigned department member (Specialized/Partner department on idea)
            if (roleClaims.Contains(RoleCodes.SpecializedDepartment) ||
                roleClaims.Contains(RoleCodes.PartnerDepartment))
            {
                var deptIdClaim = User.FindFirst(RoleCodes.DepartmentIdClaim)?.Value;
                if (!string.IsNullOrEmpty(deptIdClaim) && Guid.TryParse(deptIdClaim, out var deptId))
                {
                    var idea = await _db.InnovationIdeas
                        .AsNoTracking()
                        .Where(i => i.Id == ideaId)
                        .Select(i => (Guid?)i.AssignedDepartmentId)
                        .FirstOrDefaultAsync(ct);
                    return idea.HasValue && idea.Value == deptId;
                }
            }

            return false;
        }
    }
}
