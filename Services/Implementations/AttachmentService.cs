using Ibtikar.Models;
using Ibtikar.Repositories;
using Ibtikar.Services.Helpers;
using Microsoft.Extensions.Options;

namespace Ibtikar.Services.Implementations
{
    public sealed class AttachmentService
    {
        private readonly IAttachmentRepository _repo;
        private readonly FileStorageService _storage;
        private readonly PdfValidator _pdf;
        private readonly IntegrationOptions _options;
        private readonly ILogger<AttachmentService> _logger;

        public AttachmentService(
            IAttachmentRepository repo,
            FileStorageService storage,
            PdfValidator pdf,
            IOptions<IntegrationOptions> options,
            ILogger<AttachmentService> logger)
        {
            _repo = repo;
            _storage = storage;
            _pdf = pdf;
            _options = options.Value;
            _logger = logger;
        }

        public int MaxCount => Math.Max(0, _options.AttachmentMaxCount);
        public long MaxBytes => Math.Max(0L, _options.AttachmentMaxBytes);

        private string DraftRoot(Guid userId)
            => Path.Combine(_storage.Root, "_drafts", userId.ToString("N"));

        private string DraftFolder(Guid userId, Guid draftId)
            => Path.Combine(DraftRoot(userId), draftId.ToString("N"));

        public async Task<AttachmentSaveResult> SaveAsync(
            Guid ideaId,
            Guid uploadedByUserId,
            IFormFile file,
            CancellationToken ct)
        {
            if (file is null || file.Length == 0)
                return AttachmentSaveResult.Failed("لم يتم اختيار ملف.");
            if (file.Length > MaxBytes)
                return AttachmentSaveResult.Failed($"تجاوزت الحد المسموح ({MaxBytes / 1024 / 1024} ميجا).");

            await using var probe = file.OpenReadStream();
            if (!_pdf.IsPdf(probe))
                return AttachmentSaveResult.Failed("يجب أن يكون الملف بصيغة PDF.");

            var existing = await _repo.CountForIdeaAsync(ideaId, ct);
            if (existing >= MaxCount)
                return AttachmentSaveResult.Failed($"الحد الأقصى {MaxCount} ملف لكل فكرة.");

            var storedPath = _storage.BuildStoredPath(ideaId, file.FileName);
            await using (var src = file.OpenReadStream())
            {
                await _storage.SaveAsync(storedPath, src, ct);
            }

            var attachment = new IdeaAttachment
            {
                Id = Guid.NewGuid(),
                InnovationIdeaId = ideaId,
                FileName = file.FileName,
                ContentType = "application/pdf",
                SizeBytes = file.Length,
                StoragePath = storedPath,
                UploadedAt = DateTime.UtcNow,
                UploadedByUserId = uploadedByUserId
            };
            _repo.Add(attachment);
            await _repo.SaveChangesAsync(ct);

            _logger.LogInformation("Saved attachment {Id} for idea {Idea}", attachment.Id, ideaId);
            return AttachmentSaveResult.Ok(attachment.Id, file.FileName, file.Length);
        }

        public Task<List<IdeaAttachment>> ListForIdeaAsync(Guid ideaId, CancellationToken ct)
            => _repo.ListForIdeaAsync(ideaId, ct);

        public Task<int> CountForIdeaAsync(Guid ideaId, CancellationToken ct)
            => _repo.CountForIdeaAsync(ideaId, ct);

        public Task<bool> UserOwnsIdeaAsync(Guid ideaId, Guid userId, CancellationToken ct)
            => _repo.UserOwnsIdeaAsync(ideaId, userId, ct);

        public Task<IdeaAttachment?> GetByIdAsync(Guid attachmentId, CancellationToken ct)
            => _repo.GetByIdAsync(attachmentId, ct);

        public async Task<bool> DeleteForApplicantAsync(Guid ideaId, Guid attachmentId, Guid userId, CancellationToken ct)
        {
            var idea = await _repo.GetOwnedIdeaAsync(ideaId, userId, ct);
            if (idea is null) return false;

            if (!idea.IsDraft)
            {
                var allowedStatusIds = await _repo.GetDeletableStatusIdsAsync(ct);
                if (!allowedStatusIds.Contains(idea.CurrentStatusId)) return false;
            }

            var attachment = await _repo.GetByIdForIdeaAsync(attachmentId, ideaId, ct);
            if (attachment is null) return false;

            _storage.Delete(attachment.StoragePath);
            await _repo.RemoveAndSaveAsync(attachment, ct);
            return true;
        }

        public async Task<bool> CanAccessIdeaAsync(
            Guid ideaId,
            Guid userId,
            IReadOnlyCollection<string> roleCodes,
            Guid? departmentId,
            CancellationToken ct)
        {
            if (await _repo.UserOwnsIdeaAsync(ideaId, userId, ct)) return true;

            var roles = new HashSet<string>(roleCodes, StringComparer.OrdinalIgnoreCase);
            if (roles.Contains(RoleCodes.SystemAdmin)
                || roles.Contains(RoleCodes.AuditEmployee)
                || roles.Contains(RoleCodes.SpecializedDepartment)
                || roles.Contains(RoleCodes.PartnerDepartment)
                || roles.Contains(RoleCodes.InnovationCommitteeMember))
                return true;

            if (departmentId.HasValue
                && (roles.Contains(RoleCodes.SpecializedDepartment) || roles.Contains(RoleCodes.PartnerDepartment)))
            {
                var assigned = await _repo.GetIdeaAssignedDepartmentIdAsync(ideaId, ct);
                if (assigned.HasValue && assigned.Value == departmentId.Value) return true;
            }

            return false;
        }

        public AttachmentSaveResult SaveDraftAsync(Guid userId, Guid draftId, IFormFile file, CancellationToken ct)
        {
            if (file is null || file.Length == 0)
                return AttachmentSaveResult.Failed("لم يتم اختيار ملف.");
            if (file.Length > MaxBytes)
                return AttachmentSaveResult.Failed($"تجاوزت الحد المسموح ({MaxBytes / 1024 / 1024} ميجا).");

            using (var probe = file.OpenReadStream())
            {
                if (!_pdf.IsPdf(probe))
                    return AttachmentSaveResult.Failed("يجب أن يكون الملف بصيغة PDF.");
            }

            var folder = DraftFolder(userId, draftId);
            Directory.CreateDirectory(folder);

            var existing = Directory.Exists(folder)
                ? Directory.GetFiles(folder).Length
                : 0;
            if (existing >= MaxCount)
                return AttachmentSaveResult.Failed($"الحد الأقصى {MaxCount} ملف لكل فكرة.");

            var extension = Path.GetExtension(file.FileName);
            var storedFileName = Guid.NewGuid().ToString("N") + (string.IsNullOrEmpty(extension) ? ".pdf" : extension.ToLowerInvariant());
            var storedPath = Path.Combine(folder, storedFileName);

            using (var src = file.OpenReadStream())
            using (var fs = new FileStream(storedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: false))
            {
                src.CopyTo(fs);
            }

            _logger.LogInformation("Saved draft attachment {Name} for user {User} draft {Draft}", storedFileName, userId, draftId);
            return AttachmentSaveResult.Ok(Guid.NewGuid(), file.FileName, file.Length);
        }

        public IReadOnlyList<DraftAttachmentInfo> ListDraftAsync(Guid userId, Guid draftId)
        {
            var folder = DraftFolder(userId, draftId);
            if (!Directory.Exists(folder)) return Array.Empty<DraftAttachmentInfo>();

            return Directory.GetFiles(folder)
                .Select(p => new DraftAttachmentInfo(
                    Guid.NewGuid(),
                    Path.GetFileName(p),
                    new FileInfo(p).Length,
                    new FileInfo(p).CreationTimeUtc))
                .OrderBy(x => x.UploadedAt)
                .ToList();
        }

        public int CountDraftAsync(Guid userId, Guid draftId)
        {
            var folder = DraftFolder(userId, draftId);
            return Directory.Exists(folder) ? Directory.GetFiles(folder).Length : 0;
        }

        public bool DeleteDraftFile(Guid userId, Guid draftId, string storedFileName)
        {
            var folder = DraftFolder(userId, draftId);
            var fullPath = Path.GetFullPath(Path.Combine(folder, storedFileName));
            if (!fullPath.StartsWith(Path.GetFullPath(folder), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path traversal attempt blocked in DeleteDraftFile: {Path}", fullPath);
                return false;
            }
            if (!File.Exists(fullPath)) return false;
            File.Delete(fullPath);
            return true;
        }

        public int MoveDraftToIdea(Guid userId, Guid draftId, Guid ideaId, CancellationToken ct)
        {
            var srcFolder = DraftFolder(userId, draftId);
            if (!Directory.Exists(srcFolder)) return 0;

            var dstFolder = Path.Combine(_storage.Root, ideaId.ToString("N"));
            Directory.CreateDirectory(dstFolder);

            var moved = 0;
            foreach (var srcPath in Directory.GetFiles(srcFolder))
            {
                var fileName = Path.GetFileName(srcPath);
                var dstPath = Path.Combine(dstFolder, fileName);

                if (!File.Exists(dstPath))
                {
                    File.Move(srcPath, dstPath);
                }
                else
                {
                    File.Copy(srcPath, dstPath, overwrite: true);
                    File.Delete(srcPath);
                }

                var originalName = fileName.Contains('_')
                    ? fileName[(fileName.IndexOf('_') + 1)..]
                    : fileName;
                var info = new FileInfo(dstPath);

                _repo.Add(new IdeaAttachment
                {
                    Id = Guid.NewGuid(),
                    InnovationIdeaId = ideaId,
                    FileName = originalName,
                    ContentType = "application/pdf",
                    SizeBytes = info.Length,
                    StoragePath = dstPath,
                    UploadedAt = info.CreationTimeUtc,
                    UploadedByUserId = userId
                });
                moved++;
            }

            try { Directory.Delete(srcFolder, recursive: true); }
            catch (Exception ex) { _logger.LogWarning(ex, "Could not delete empty draft folder {Folder}", srcFolder); }

            return moved;
        }

        public void DeleteDraftFolder(Guid userId, Guid draftId)
        {
            var folder = DraftFolder(userId, draftId);
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    public sealed record AttachmentSaveResult(bool Success, string? Error, Guid? AttachmentId, string? FileName, long SizeBytes)
    {
        public static AttachmentSaveResult Failed(string error) => new(false, error, null, null, 0);
        public static AttachmentSaveResult Ok(Guid id, string fileName, long sizeBytes) => new(true, null, id, fileName, sizeBytes);
    }

    public sealed record DraftAttachmentInfo(Guid Id, string FileName, long SizeBytes, DateTime UploadedAt);
}