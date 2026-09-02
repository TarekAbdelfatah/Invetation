using Ibtikar.Data;
using Ibtikar.DTOs.Committees;
using Ibtikar.Models;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Repositories
{
    public sealed class CommitteeRepository : ICommitteeRepository
    {
        private readonly IbtikarDbContext _db;

        public CommitteeRepository(IbtikarDbContext db) => _db = db;

        public async Task<IReadOnlyList<CommitteeSummaryDto>> GetAllAsync(CancellationToken ct)
        {
            var committees = await _db.InnovationCommittees.AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(ct);

            var committeeIds = committees.Select(c => c.Id).ToList();
            var members = await _db.CommitteeMembers.AsNoTracking()
                .Where(m => committeeIds.Contains(m.InnovationCommitteeId))
                .ToListAsync(ct);

            var heads = members
                .Where(m => m.IsHead)
                .ToDictionary(m => m.InnovationCommitteeId, m => m.UserId);
            var headNames = await _db.Users.AsNoTracking()
                .Where(u => heads.Values.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

            return committees.Select(c =>
            {
                var headUserId = heads.TryGetValue(c.Id, out var hid) ? hid : Guid.Empty;
                var headName = headUserId != Guid.Empty && headNames.TryGetValue(headUserId, out var n)
                    ? n
                    : "—";
                var count = members.Count(m => m.InnovationCommitteeId == c.Id);
                return new CommitteeSummaryDto(c.Id, c.Name, c.Description, c.IsActive, c.CreatedAt, c.ActivatedAt, headName, count);
            }).ToList();
        }

        public async Task<CommitteeMemberOptionDto[]> GetMemberCandidatesAsync(Guid? excludeCommitteeId, CancellationToken ct)
        {
            var roleCode = RoleCodes.InnovationCommitteeMember;

            var adminRecords = await _db.Admins.AsNoTracking()
                .Where(a => a.IsActive && a.Role != null && a.Role.Code == roleCode)
                .ToListAsync(ct);

            if (adminRecords.Count == 0)
            {
                return Array.Empty<CommitteeMemberOptionDto>();
            }

            var existingMemberUserIds = new HashSet<Guid>();
            if (excludeCommitteeId.HasValue)
            {
                var existing = await _db.CommitteeMembers.AsNoTracking()
                    .Where(m => m.InnovationCommitteeId == excludeCommitteeId.Value)
                    .Select(m => m.UserId)
                    .ToListAsync(ct);
                existingMemberUserIds = new HashSet<Guid>(existing);
            }

            var candidates = new List<CommitteeMemberOptionDto>();
            foreach (var admin in adminRecords)
            {
                if (string.IsNullOrWhiteSpace(admin.NetworkUser)) continue;
                var cleanUser = admin.NetworkUser.Trim();
                if (cleanUser.EndsWith("@bog.gov.sa", StringComparison.OrdinalIgnoreCase))
                {
                    cleanUser = cleanUser.Substring(0, cleanUser.Length - "@bog.gov.sa".Length);
                }

                var userId = AdminIdToGuid(admin.Id);
                if (existingMemberUserIds.Contains(userId)) continue;

                candidates.Add(new CommitteeMemberOptionDto(
                    userId,
                    cleanUser,
                    cleanUser,
                    false));
            }

            return candidates.OrderBy(c => c.FullName).ToArray();
        }

        private static Guid AdminIdToGuid(int adminId)
        {
            var bytes = new byte[16];
            BitConverter.GetBytes(adminId).CopyTo(bytes, 0);
            return new Guid(bytes);
        }

        public async Task AddCommitteeAsync(InnovationCommittee committee, CancellationToken ct)
        {
            await _db.InnovationCommittees.AddAsync(committee, ct);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<InnovationCommittee?> GetWithMembersAsync(Guid committeeId, CancellationToken ct)
            => await _db.InnovationCommittees
                .Include(c => c.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(c => c.Id == committeeId, ct);

        public async Task<bool> IsHeadAsync(Guid committeeId, Guid userId, CancellationToken ct)
            => await _db.CommitteeMembers.AsNoTracking()
                .AnyAsync(m => m.InnovationCommitteeId == committeeId
                               && m.UserId == userId
                               && m.IsHead, ct);

        public async Task<bool> IsMemberAsync(Guid committeeId, Guid userId, CancellationToken ct)
            => await _db.CommitteeMembers.AsNoTracking()
                .AnyAsync(m => m.InnovationCommitteeId == committeeId
                               && m.UserId == userId, ct);

        public async Task<bool> IsActiveMemberAsync(Guid userId, CancellationToken ct)
            => await _db.CommitteeMembers.AsNoTracking()
                .AnyAsync(m => m.UserId == userId && m.InnovationCommittee != null && m.InnovationCommittee.IsActive, ct);

        public async Task<Guid?> GetCommitteeIdForMemberAsync(Guid userId, CancellationToken ct)
            => await _db.CommitteeMembers.AsNoTracking()
                .Where(m => m.UserId == userId && m.InnovationCommittee != null && m.InnovationCommittee.IsActive)
                .Select(m => (Guid?)m.InnovationCommitteeId)
                .FirstOrDefaultAsync(ct);

        public async Task<Guid?> GetCommitteeIdForHeadAsync(Guid userId, CancellationToken ct)
            => await _db.CommitteeMembers.AsNoTracking()
                .Where(m => m.UserId == userId && m.IsHead && m.InnovationCommittee != null && m.InnovationCommittee.IsActive)
                .Select(m => (Guid?)m.InnovationCommitteeId)
                .FirstOrDefaultAsync(ct);

        public async Task<IReadOnlyList<DelegationMemberOptionDto>> GetDelegateCandidatesAsync(Guid committeeId, CancellationToken ct)
            => await _db.CommitteeMembers.AsNoTracking()
                .Where(m => m.InnovationCommitteeId == committeeId && !m.IsHead && m.User != null)
                .OrderBy(m => m.User!.FullName)
                .Select(m => new DelegationMemberOptionDto(m.UserId, m.User!.FullName, m.User!.Username))
                .ToListAsync(ct);

        public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
    }
}