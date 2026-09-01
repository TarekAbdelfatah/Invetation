using Ibtikar.Models;

namespace Ibtikar.Services.Interfaces
{
    public interface IDelegationService
    {
        Task<DelegationValidationResult> ValidateNewAsync(
            Guid innovationCommitteeId,
            Guid delegateMemberUserId,
            DateTime startAt,
            DateTime endAt,
            CancellationToken ct);

        Task<CommitteeDelegation?> GetActiveAsync(Guid innovationCommitteeId, CancellationToken ct);
        Task<bool> IsDelegateAsync(Guid userId, CancellationToken ct);
        Task<bool> HasActiveDelegationAsync(Guid delegateMemberUserId, DateTime startAt, DateTime endAt, CancellationToken ct);
        Task<Guid?> GetCommitteeIdForMemberAsync(Guid userId, CancellationToken ct);
        Task<Guid?> GetCommitteeIdForHeadAsync(Guid userId, CancellationToken ct);
        Task<IReadOnlyList<DelegationMemberOptionDto>> GetDelegateCandidatesAsync(Guid committeeId, CancellationToken ct);
        Task<IReadOnlyList<DelegationRowDto>> GetDelegationsAsync(Guid committeeId, CancellationToken ct);
        Task<DelegationCreateResultDto> AddAsync(Guid committeeId, Guid headUserId, Guid delegateMemberUserId, DateTime startAt, DateTime endAt, CancellationToken ct);
        Task<DelegationCreateResultDto> CancelAsync(Guid committeeId, Guid headUserId, Guid delegationId, CancellationToken ct);
    }

    public sealed record DelegationValidationResult(bool Ok, string? Error)
    {
        public static DelegationValidationResult Success() => new(true, null);
        public static DelegationValidationResult Fail(string error) => new(false, error);
    }

    public sealed record DelegationMemberOptionDto(Guid UserId, string FullName, string Username);

    public sealed record DelegationRowDto(
        Guid Id,
        string DelegateName,
        DateTime StartAt,
        DateTime EndAt,
        bool IsActive);

    public sealed record DelegationCreateResultDto(bool Success, string Message);
}
