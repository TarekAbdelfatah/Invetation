using Ibtikar.Data;
using Ibtikar.Models;
using Ibtikar.Services.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Repositories
{
    public sealed class AttachmentRepository : IAttachmentRepository
    {
        private readonly IbtikarDbContext _db;

        public AttachmentRepository(IbtikarDbContext db) => _db = db;

        public Task<int> CountForIdeaAsync(Guid ideaId, CancellationToken ct)
            => _db.IdeaAttachments.CountAsync(a => a.InnovationIdeaId == ideaId, ct);

        public async Task<List<IdeaAttachment>> ListForIdeaAsync(Guid ideaId, CancellationToken ct)
            => await _db.IdeaAttachments
                .AsNoTracking()
                .Where(a => a.InnovationIdeaId == ideaId)
                .OrderBy(a => a.UploadedAt)
                .ToListAsync(ct);

        public async Task<IReadOnlyList<IdeaAttachment>> GetByIdsForIdeaAsync(Guid ideaId, IReadOnlyCollection<Guid> ids, CancellationToken ct)
            => await _db.IdeaAttachments
                .AsNoTracking()
                .Where(a => ids.Contains(a.Id) && a.InnovationIdeaId == ideaId)
                .ToListAsync(ct);

        public async Task<IdeaAttachment?> GetByIdAsync(Guid attachmentId, CancellationToken ct)
            => await _db.IdeaAttachments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);

        public async Task<IdeaAttachment?> GetByIdForIdeaAsync(Guid attachmentId, Guid ideaId, CancellationToken ct)
            => await _db.IdeaAttachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.InnovationIdeaId == ideaId, ct);

        public async Task<bool> UserOwnsIdeaAsync(Guid ideaId, Guid userId, CancellationToken ct)
            => await _db.InnovationIdeas
                .AsNoTracking()
                .AnyAsync(i => i.Id == ideaId && i.ApplicantUserId == userId, ct);

        public async Task<Guid?> GetIdeaAssignedDepartmentIdAsync(Guid ideaId, CancellationToken ct)
            => await _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => i.Id == ideaId)
                .Select(i => (Guid?)i.AssignedDepartmentId)
                .FirstOrDefaultAsync(ct);

        public async Task<IReadOnlyList<Guid>> GetDeletableStatusIdsAsync(CancellationToken ct)
            => await _db.IdeaStatuses
                .AsNoTracking()
                .Where(s => s.Code == IdeaStatusCodes.WaitingForCompletion
                         || s.Code == IdeaStatusCodes.ReturnedForDevelopment)
                .Select(s => s.Id)
                .ToListAsync(ct);

        public async Task<InnovationIdea?> GetOwnedIdeaAsync(Guid ideaId, Guid userId, CancellationToken ct)
            => await _db.InnovationIdeas
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == ideaId && i.ApplicantUserId == userId, ct);

        public void Add(IdeaAttachment attachment)
            => _db.IdeaAttachments.Add(attachment);

        public async Task AddAndSaveAsync(IdeaAttachment attachment, CancellationToken ct)
        {
            await _db.IdeaAttachments.AddAsync(attachment, ct);
            await _db.SaveChangesAsync(ct);
        }

        public async Task RemoveAndSaveAsync(IdeaAttachment attachment, CancellationToken ct)
        {
            _db.IdeaAttachments.Remove(attachment);
            await _db.SaveChangesAsync(ct);
        }

        public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
    }
}