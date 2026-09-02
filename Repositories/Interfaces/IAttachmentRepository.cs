using Ibtikar.Models;

namespace Ibtikar.Repositories
{
    public interface IAttachmentRepository
    {
        Task<int> CountForIdeaAsync(Guid ideaId, CancellationToken ct);
        Task<List<IdeaAttachment>> ListForIdeaAsync(Guid ideaId, CancellationToken ct);
        Task<IReadOnlyList<IdeaAttachment>> GetByIdsForIdeaAsync(Guid ideaId, IReadOnlyCollection<Guid> ids, CancellationToken ct);
        Task<IdeaAttachment?> GetByIdAsync(Guid attachmentId, CancellationToken ct);
        Task<IdeaAttachment?> GetByIdForIdeaAsync(Guid attachmentId, Guid ideaId, CancellationToken ct);
        Task<bool> UserOwnsIdeaAsync(Guid ideaId, Guid userId, CancellationToken ct);
        Task<Guid?> GetIdeaAssignedDepartmentIdAsync(Guid ideaId, CancellationToken ct);
        Task<IReadOnlyList<Guid>> GetDeletableStatusIdsAsync(CancellationToken ct);
        Task<InnovationIdea?> GetOwnedIdeaAsync(Guid ideaId, Guid userId, CancellationToken ct);
        void Add(IdeaAttachment attachment);
        Task AddAndSaveAsync(IdeaAttachment attachment, CancellationToken ct);
        Task RemoveAndSaveAsync(IdeaAttachment attachment, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}