using Ibtikar.Data;
using Ibtikar.Models;
using Ibtikar.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ibtikar.Services.Implementations
{
    public sealed class AttachmentService
    {
        private readonly IbtikarDbContext _db;
        private readonly FileStorageService _storage;
        private readonly PdfValidator _pdf;
        private readonly IntegrationOptions _options;
        private readonly ILogger<AttachmentService> _logger;

        public AttachmentService(
            IbtikarDbContext db,
            FileStorageService storage,
            PdfValidator pdf,
            IOptions<IntegrationOptions> options,
            ILogger<AttachmentService> logger)
        {
            _db = db;
            _storage = storage;
            _pdf = pdf;
            _options = options.Value;
            _logger = logger;
        }

        public int MaxCount => Math.Max(0, _options.AttachmentMaxCount);
        public long MaxBytes => Math.Max(0L, _options.AttachmentMaxBytes);

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

            var existing = await _db.IdeaAttachments.CountAsync(a => a.InnovationIdeaId == ideaId, ct);
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
                ContentType = file.ContentType ?? "application/pdf",
                SizeBytes = file.Length,
                StoragePath = storedPath,
                UploadedAt = DateTime.UtcNow,
                UploadedByUserId = uploadedByUserId
            };
            _db.IdeaAttachments.Add(attachment);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Saved attachment {Id} for idea {Idea}", attachment.Id, ideaId);
            return AttachmentSaveResult.Ok(attachment.Id, file.FileName, file.Length);
        }

        public async Task<List<IdeaAttachment>> ListForIdeaAsync(Guid ideaId, CancellationToken ct)
        {
            return await _db.IdeaAttachments
                .AsNoTracking()
                .Where(a => a.InnovationIdeaId == ideaId)
                .OrderBy(a => a.UploadedAt)
                .ToListAsync(ct);
        }

        public Task<int> CountForIdeaAsync(Guid ideaId, CancellationToken ct)
            => _db.IdeaAttachments.CountAsync(a => a.InnovationIdeaId == ideaId, ct);

        public async Task<bool> UserOwnsIdeaAsync(Guid ideaId, Guid userId, CancellationToken ct)
            => await _db.InnovationIdeas
                .AsNoTracking()
                .AnyAsync(i => i.Id == ideaId && i.ApplicantUserId == userId, ct);
    }

    public sealed record AttachmentSaveResult(bool Success, string? Error, Guid? AttachmentId, string? FileName, long SizeBytes)
    {
        public static AttachmentSaveResult Failed(string error) => new(false, error, null, null, 0);
        public static AttachmentSaveResult Ok(Guid id, string fileName, long sizeBytes) => new(true, null, id, fileName, sizeBytes);
    }
}
