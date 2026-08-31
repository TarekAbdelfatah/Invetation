using Ibtikar.Data;
using Ibtikar.DTOs.Committee;
using Ibtikar.Services.Ideas;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Services.Committee
{
    public sealed class CommitteeDashboardService : ICommitteeDashboardService
    {
        private readonly IbtikarDbContext _db;

        public CommitteeDashboardService(IbtikarDbContext db)
        {
            _db = db;
        }

        public async Task<CommitteeDashboardDto> GetSnapshotAsync(Guid userId, CancellationToken ct)
        {
            var committeeId = await GetCommitteeIdForMemberAsync(userId, ct);
            if (committeeId is null)
            {
                return new CommitteeDashboardDto(0, 0, 0, 0);
            }

            var underStudy = await _db.InnovationIdeas.AsNoTracking()
                .CountAsync(i => i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.ReferredCommittee, ct);

            var underVoting = await _db.InnovationIdeas.AsNoTracking()
                .CountAsync(i => i.CurrentStatus != null
                    && (i.CurrentStatus.Code == IdeaStatusCodes.ReferredCommittee
                        || i.CurrentStatus.Code == IdeaStatusCodes.UnderAssessment), ct);

            var accepted = await _db.InnovationIdeas.AsNoTracking()
                .CountAsync(i => i.CurrentStatus != null
                    && (i.CurrentStatus.Code == IdeaStatusCodes.Approved
                        || i.CurrentStatus.Code == IdeaStatusCodes.InExecution), ct);

            var rejected = await _db.InnovationIdeas.AsNoTracking()
                .CountAsync(i => i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.Rejected, ct);

            return new CommitteeDashboardDto(underStudy, underVoting, accepted, rejected);
        }

        public async Task<CommitteeReferralsDto?> GetReferralsAsync(Guid userId, string statusFilter, CancellationToken ct)
        {
            var committeeId = await GetCommitteeIdForMemberAsync(userId, ct);
            if (committeeId is null) return null;

            var query = _db.InnovationIdeas.AsNoTracking()
                .Where(i => i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.ReferredCommittee);

            query = statusFilter switch
            {
                "accepted" => query.Where(i => i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.Approved),
                "rejected" => query.Where(i => i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.Rejected),
                _ => query
            };

            var now = DateTime.UtcNow;
            var rows = await query
                .OrderByDescending(i => i.CreatedAt)
                .Take(100)
                .Select(i => new CommitteeReferralRowDto(
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    i.CurrentStatus != null ? i.CurrentStatus.Code : string.Empty,
                    i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    i.CurrentStatus != null ? i.CurrentStatus.Color : "#6c757d",
                    i.ApplicantUser != null ? i.ApplicantUser.FullName : null,
                    i.ApplicantDepartment != null ? i.ApplicantDepartment.Name : null,
                    i.AuditAssignedAt,
                    i.AuditAssignedAt.HasValue ? (now - i.AuditAssignedAt.Value).TotalDays : 0.0,
                    i.AuditAssignedAt.HasValue && (now - i.AuditAssignedAt.Value) > TimeSpan.FromDays(4)))
                .ToListAsync(ct);

            return new CommitteeReferralsDto(rows, statusFilter);
        }

        public Task<bool> IsActiveCommitteeMemberAsync(Guid userId, CancellationToken ct)
        {
            return _db.CommitteeMembers.AsNoTracking()
                .AnyAsync(m => m.UserId == userId && m.InnovationCommittee != null && m.InnovationCommittee.IsActive, ct);
        }

        private async Task<Guid?> GetCommitteeIdForMemberAsync(Guid userId, CancellationToken ct)
        {
            return await _db.CommitteeMembers.AsNoTracking()
                .Where(m => m.UserId == userId && m.InnovationCommittee != null && m.InnovationCommittee.IsActive)
                .Select(m => (Guid?)m.InnovationCommitteeId)
                .FirstOrDefaultAsync(ct);
        }
    }
}
