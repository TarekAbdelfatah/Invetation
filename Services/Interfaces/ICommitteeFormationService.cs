using Ibtikar.DTOs.Committees;

namespace Ibtikar.Services.Interfaces
{
    public interface ICommitteeFormationService
    {
        Task<IReadOnlyList<CommitteeSummaryDto>> GetAllAsync(CancellationToken ct);
        Task<CommitteeDetailDto?> GetDetailAsync(Guid committeeId, CancellationToken ct);
        Task<CommitteeMemberOptionDto[]> GetMemberCandidatesAsync(Guid? excludeCommitteeId, CancellationToken ct);
        Task<CommitteeCreateResultDto> CreateAsync(Guid actorUserId, CommitteeCreateDto dto, CancellationToken ct);
        Task<CommitteeEditResultDto> UpdateAsync(Guid actorUserId, CommitteeEditDto dto, CancellationToken ct);
        Task<CommitteeCreateResultDto> ActivateAsync(Guid actorUserId, Guid committeeId, CancellationToken ct);
    }
}
