using Ibtikar.Models;

namespace Ibtikar.Services.Committee
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
    }

    public sealed record DelegationValidationResult(bool Ok, string? Error)
    {
        public static DelegationValidationResult Success() => new(true, null);
        public static DelegationValidationResult Fail(string error) => new(false, error);
    }
}
