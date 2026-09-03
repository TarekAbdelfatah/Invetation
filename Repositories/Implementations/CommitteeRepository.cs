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
                .Where(m => m.InnovationCommitteeId != null && committeeIds.Contains(m.InnovationCommitteeId.Value))
                .ToListAsync(ct);

            var heads = members
                .Where(m => m.IsHead)
                .ToDictionary(m => m.InnovationCommitteeId!.Value, m => m.AdminId);

            var headNames = await ResolveAdminNamesAsync(heads.Values, ct);

            return committees.Select(c =>
            {
                var headAdminId = heads.TryGetValue(c.Id, out var hid) ? hid : 0;
                var headName = headAdminId != 0 && headNames.TryGetValue(headAdminId, out var n)
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

            var existingMemberAdminIds = new HashSet<int>();
            if (excludeCommitteeId.HasValue)
            {
                var existing = await _db.CommitteeMembers.AsNoTracking()
                    .Where(m => m.InnovationCommitteeId == excludeCommitteeId.Value)
                    .Select(m => m.AdminId)
                    .ToListAsync(ct);
                existingMemberAdminIds = new HashSet<int>(existing);
            }

            var names = await ResolveAdminNamesAsync(adminRecords.Select(a => a.Id), ct);

            var candidates = new List<CommitteeMemberOptionDto>();
            foreach (var admin in adminRecords)
            {
                if (string.IsNullOrWhiteSpace(admin.NetworkUser)) continue;
                var cleanUser = NormalizeNetworkUser(admin.NetworkUser);
                if (string.IsNullOrWhiteSpace(cleanUser)) continue;

                if (existingMemberAdminIds.Contains(admin.Id)) continue;

                var fullName = names.TryGetValue(admin.Id, out var name) ? name : cleanUser;

                candidates.Add(new CommitteeMemberOptionDto(
                    admin.Id,
                    fullName,
                    cleanUser,
                    false));
            }

            return candidates.OrderBy(c => c.FullName).ToArray();
        }

        public async Task<HashSet<int>> GetActiveCommitteeMemberIdsAsync(IReadOnlyCollection<int> adminIds, CancellationToken ct)
        {
            if (adminIds is null || adminIds.Count == 0)
                return new HashSet<int>();

            var roleCode = RoleCodes.InnovationCommitteeMember;

            var active = await _db.Admins.AsNoTracking()
                .Where(a => a.IsActive && a.Role != null && a.Role.Code == roleCode)
                .Select(a => a.Id)
                .ToListAsync(ct);

            var activeSet = new HashSet<int>(active);
            activeSet.IntersectWith(adminIds);

            return activeSet;
        }

        public async Task AddCommitteeAsync(InnovationCommittee committee, CancellationToken ct)
        {
            await _db.InnovationCommittees.AddAsync(committee, ct);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<InnovationCommittee?> GetWithMembersAsync(Guid committeeId, CancellationToken ct)
            => await _db.InnovationCommittees
                .Include(c => c.Members)
                .ThenInclude(m => m.Admin)
                .FirstOrDefaultAsync(c => c.Id == committeeId, ct);

        public async Task<bool> IsHeadAsync(Guid committeeId, Guid userId, CancellationToken ct)
        {
            var adminId = await ResolveAdminIdFromUserIdAsync(userId, ct: ct);
            if (adminId is null) return false;

            return await _db.CommitteeMembers.AsNoTracking()
                .AnyAsync(m => m.InnovationCommitteeId == committeeId
                               && m.AdminId == adminId.Value
                               && m.IsHead, ct);
        }

        public async Task<bool> IsMemberAsync(Guid committeeId, Guid userId, CancellationToken ct)
        {
            var adminId = await ResolveAdminIdFromUserIdAsync(userId, ct: ct);
            if (adminId is null) return false;

            return await _db.CommitteeMembers.AsNoTracking()
                .AnyAsync(m => m.InnovationCommitteeId == committeeId
                               && m.AdminId == adminId.Value, ct);
        }

        public async Task<bool> IsActiveMemberAsync(Guid userId, CancellationToken ct)
        {
            var adminId = await ResolveAdminIdFromUserIdAsync(userId, ct: ct);
            if (adminId is null) return false;

            return await _db.CommitteeMembers.AsNoTracking()
                .AnyAsync(m => m.AdminId == adminId.Value && m.InnovationCommittee != null && m.InnovationCommittee.IsActive, ct);
        }

        public async Task<Guid?> GetCommitteeIdForMemberAsync(Guid userId, CancellationToken ct)
        {
            var adminId = await ResolveAdminIdFromUserIdAsync(userId, RoleCodes.InnovationCommitteeMember, ct);
            if (adminId is null) return null;

            return await _db.CommitteeMembers.AsNoTracking()
                .Where(m => m.AdminId == adminId.Value && m.InnovationCommittee != null && m.InnovationCommittee.IsActive)
                .Select(m => (Guid?)m.InnovationCommitteeId)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<Guid?> GetCommitteeIdForHeadAsync(Guid userId, CancellationToken ct)
        {
            var adminId = await ResolveAdminIdFromUserIdAsync(userId, RoleCodes.InnovationCommitteeHead, ct);
            if (adminId is null) return null;

            return await _db.CommitteeMembers.AsNoTracking()
                .Where(m => m.AdminId == adminId.Value && m.InnovationCommittee != null && m.InnovationCommittee.IsActive)
                .Select(m => (Guid?)m.InnovationCommitteeId)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IReadOnlyList<DelegationMemberOptionDto>> GetDelegateCandidatesAsync(Guid committeeId, CancellationToken ct)
        {
            var members = await _db.CommitteeMembers.AsNoTracking()
                .Where(m => m.InnovationCommitteeId == committeeId && !m.IsHead && m.Admin != null)
                .Select(m => new { m.AdminId, m.Admin!.NetworkUser })
                .ToListAsync(ct);

            var names = await ResolveAdminNamesAsync(members.Select(m => m.AdminId), ct);

            return members
                .OrderBy(m => names.TryGetValue(m.AdminId, out var n) ? n : m.NetworkUser ?? string.Empty)
                .Select(m => new DelegationMemberOptionDto(
                    AdminIdToGuid(m.AdminId),
                    names.TryGetValue(m.AdminId, out var name) ? name : (m.NetworkUser ?? string.Empty),
                    NormalizeNetworkUser(m.NetworkUser ?? string.Empty)))
                .ToList();
        }

        public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

        private async Task<int?> ResolveAdminIdFromUserIdAsync(Guid userId, string? requiredRoleCode = null, CancellationToken ct = default)
        {
            var username = await _db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.Username)
                .FirstOrDefaultAsync(ct);

            if (string.IsNullOrWhiteSpace(username)) return null;

            var query = _db.Admins.AsNoTracking()
                .Where(a => a.NetworkUser == username && a.IsActive);

            if (!string.IsNullOrWhiteSpace(requiredRoleCode))
            {
                query = query.Where(a => a.Role != null && a.Role.Code == requiredRoleCode);
            }

            var adminId = await query.Select(a => (int?)a.Id).FirstOrDefaultAsync(ct);
            return adminId;
        }

        private async Task<Dictionary<int, string>> ResolveAdminNamesAsync(IEnumerable<int> adminIds, CancellationToken ct)
        {
            var ids = adminIds.Distinct().ToList();
            var result = new Dictionary<int, string>();

            if (ids.Count == 0) return result;

            var admins = await _db.Admins.AsNoTracking()
                .Where(a => ids.Contains(a.Id))
                .Select(a => new { a.Id, a.NetworkUser })
                .ToListAsync(ct);

            var adminNetworkUsers = admins
                .Select(a => NormalizeNetworkUser(a.NetworkUser ?? string.Empty))
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct()
                .ToList();

            var employeeNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (adminNetworkUsers.Count > 0)
            {
                var employees = await _commonDb.Employees.AsNoTracking()
                    .Where(e => e.NetworkUser != null && e.NetworkUser != "")
                    .Select(e => new { e.NetworkUser, e.Name })
                    .ToListAsync(ct);

                foreach (var emp in employees)
                {
                    var key = NormalizeNetworkUser(emp.NetworkUser);
                    if (!string.IsNullOrWhiteSpace(key) && !employeeNames.ContainsKey(key))
                        employeeNames[key] = string.IsNullOrWhiteSpace(emp.Name) ? key : emp.Name.Trim();
                }
            }

            foreach (var admin in admins)
            {
                var clean = NormalizeNetworkUser(admin.NetworkUser ?? string.Empty);
                var name = employeeNames.TryGetValue(clean, out var n) ? n : (string.IsNullOrWhiteSpace(clean) ? admin.NetworkUser : clean);
                result[admin.Id] = name;
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
    }
}
