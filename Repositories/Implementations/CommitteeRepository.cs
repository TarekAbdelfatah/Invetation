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
        private readonly CommonSysDbContext _commonDb;

        public CommitteeRepository(IbtikarDbContext db, CommonSysDbContext commonDb)
        {
            _db = db;
            _commonDb = commonDb;
        }

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

            var cleanUsers = adminRecords
                .Select(a => a.NetworkUser?.Trim())
                .Where(nu => !string.IsNullOrWhiteSpace(nu))
                .Distinct()
                .ToList();

            var employees = await _commonDb.Employees.AsNoTracking()
                .Where(e => e.NetworkUser != null && e.NetworkUser != "")
                .Select(e => new { e.NetworkUser, e.Name })
                .ToListAsync(ct);

            var employeeNameByUser = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var emp in employees)
            {
                var key = NormalizeNetworkUser(emp.NetworkUser);
                if (!string.IsNullOrWhiteSpace(key) && !employeeNameByUser.ContainsKey(key))
                    employeeNameByUser[key] = string.IsNullOrWhiteSpace(emp.Name) ? key : emp.Name.Trim();
            }

            var candidates = new List<CommitteeMemberOptionDto>();
            foreach (var admin in adminRecords)
            {
                if (string.IsNullOrWhiteSpace(admin.NetworkUser)) continue;
                var cleanUser = NormalizeNetworkUser(admin.NetworkUser);
                if (string.IsNullOrWhiteSpace(cleanUser)) continue;

                var userId = AdminIdToGuid(admin.Id);
                if (existingMemberUserIds.Contains(userId)) continue;

                var fullName = employeeNameByUser.TryGetValue(cleanUser, out var name) ? name : cleanUser;

                candidates.Add(new CommitteeMemberOptionDto(
                    userId,
                    fullName,
                    cleanUser,
                    false));
            }

            return candidates.OrderBy(c => c.FullName).ToArray();
        }

        public async Task<HashSet<Guid>> GetActiveCommitteeMemberIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
        {
            if (ids is null || ids.Count == 0)
                return new HashSet<Guid>();

            var roleCode = RoleCodes.InnovationCommitteeMember;

            var activeAdminIds = await _db.Admins.AsNoTracking()
                .Where(a => a.IsActive && a.Role != null && a.Role.Code == roleCode)
                .Select(a => a.Id)
                .ToListAsync(ct);

            var activeAdminUserIdSet = new HashSet<Guid>(activeAdminIds.Select(AdminIdToGuid));

            var result = new HashSet<Guid>();
            foreach (var id in ids)
            {
                if (activeAdminUserIdSet.Contains(id))
                    result.Add(id);
            }

            return result;
        }

        private static string NormalizeNetworkUser(string networkUser)
        {
            var clean = networkUser.Trim();
            if (clean.EndsWith("@bog.gov.sa", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(0, clean.Length - "@bog.gov.sa".Length);
            }
            return clean;
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