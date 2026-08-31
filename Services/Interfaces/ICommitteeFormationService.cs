using Ibtikar.DTOs.Committees;

namespace Ibtikar.Services.Interfaces
{
    public interface ICommitteeFormationService
    {
        Task<IReadOnlyList<CommitteeSummaryDto>> GetAllAsync(CancellationToken ct);
        Task<CommitteeMemberOptionDto[]> GetMemberCandidatesAsync(Guid? excludeCommitteeId, CancellationToken ct);
        Task<CommitteeCreateResultDto> CreateAsync(Guid actorUserId, CommitteeCreateDto dto, CancellationToken ct);
    }
}
