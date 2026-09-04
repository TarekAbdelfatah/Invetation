using System.Security.Cryptography;
using System.Text;
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

        public async Task<CommitteeDetailDto?> GetDetailAsync(Guid committeeId, CancellationToken ct)
        {
            var committee = await _db.InnovationCommittees.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == committeeId, ct);

            if (committee is null) return null;

            var members = await _db.CommitteeMembers.AsNoTracking()
                .Where(m => m.InnovationCommitteeId == committeeId)
                .ToListAsync(ct);

            var userIds = members.Select(m => m.UserId).Distinct().ToList();
            var users = await _db.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u, ct);

            var headMember = members.FirstOrDefault(m => m.IsHead);
            var headUserId = headMember?.UserId ?? Guid.Empty;
            var headUser = headUserId != Guid.Empty && users.TryGetValue(headUserId, out var hu) ? hu : null;

            var memberDetails = members
                .OrderBy(m => m.IsHead ? 0 : 1)
                .ThenBy(m =>
                    users.TryGetValue(m.UserId, out var u) ? u.FullName : string.Empty)
                .Select(m =>
                {
                    var u = users.TryGetValue(m.UserId, out var usr) ? usr : null;
                    return new CommitteeMemberDetailDto(
                        m.UserId,
                        u?.FullName ?? "—",
                        u?.Username ?? "—",
                        m.IsHead);
                })
                .ToList();

            return new CommitteeDetailDto(
                committee.Id,
                committee.Name,
                committee.Description,
                committee.IsActive,
                committee.CreatedAt,
                committee.ActivatedAt,
                headUserId,
                headUser?.FullName ?? "—",
                headUser?.Username ?? "—",
                memberDetails);
        }

        public async Task<CommitteeMemberOptionDto[]> GetMemberCandidatesAsync(Guid? excludeCommitteeId, CancellationToken ct)
        {
            var employees = await _commonDb.Employees.AsNoTracking()
                .Where(e => e.NetworkUser != null && e.NetworkUser != "")
                .Select(e => new { e.NetworkUser, e.Name })
                .ToListAsync(ct);

            if (employees.Count == 0)
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
            foreach (var emp in employees)
            {
                var cleanUser = NormalizeNetworkUser(emp.NetworkUser);
                if (string.IsNullOrWhiteSpace(cleanUser)) continue;

                var userId = NetworkUserToGuid(cleanUser);
                if (existingMemberUserIds.Contains(userId)) continue;

                var fullName = string.IsNullOrWhiteSpace(emp.Name) ? cleanUser : emp.Name.Trim();

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

            var employees = await _commonDb.Employees.AsNoTracking()
                .Where(e => e.NetworkUser != null && e.NetworkUser != "")
                .Select(e => e.NetworkUser)
                .ToListAsync(ct);

            var activeEmployeeGuidSet = new HashSet<Guid>(
                employees
                    .Where(nu => !string.IsNullOrWhiteSpace(nu))
                    .Select(nu => NetworkUserToGuid(NormalizeNetworkUser(nu))));

            var result = new HashSet<Guid>();
            foreach (var id in ids)
            {
                if (activeEmployeeGuidSet.Contains(id))
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

        private static Guid NetworkUserToGuid(string networkUser)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(networkUser));
            var bytes = new byte[16];
            Array.Copy(hash, bytes, 16);
            return new Guid(bytes);
        }

        public async Task EnsureUsersExistAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct)
        {
            if (userIds is null || userIds.Count == 0) return;

            var existingIds = await _db.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => u.Id)
                .ToHashSetAsync(ct);

            var missing = userIds.Where(id => !existingIds.Contains(id)).ToList();
            if (missing.Count == 0) return;

            var employees = await _commonDb.Employees.AsNoTracking()
                .Where(e => e.NetworkUser != null && e.NetworkUser != "")
                .Select(e => new { e.NetworkUser, e.Name })
                .ToListAsync(ct);

            var empByGuid = new Dictionary<Guid, (string NetworkUser, string? Name)>();
            foreach (var emp in employees)
            {
                var clean = NormalizeNetworkUser(emp.NetworkUser);
                if (string.IsNullOrWhiteSpace(clean)) continue;
                var guid = NetworkUserToGuid(clean);
                if (!empByGuid.ContainsKey(guid))
                    empByGuid[guid] = (clean, emp.Name);
            }

            foreach (var userId in missing)
            {
                if (!empByGuid.TryGetValue(userId, out var empInfo)) continue;

                var user = new User
                {
                    Id = userId,
                    Username = empInfo.NetworkUser,
                    FullName = string.IsNullOrWhiteSpace(empInfo.Name) ? empInfo.NetworkUser : empInfo.Name.Trim(),
                    Email = empInfo.NetworkUser,
                    PasswordHash = string.Empty,
                    PasswordSalt = string.Empty,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Users.Add(user);
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task AddCommitteeAsync(InnovationCommittee committee, CancellationToken ct)
        {
            await _db.InnovationCommittees.AddAsync(committee, ct);
            await _db.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(InnovationCommittee committee, IReadOnlyList<CommitteeMember> newMembers, CancellationToken ct)
        {
            var existing = await _db.CommitteeMembers
                .Where(m => m.InnovationCommitteeId == committee.Id)
                .ToListAsync(ct);

            _db.CommitteeMembers.RemoveRange(existing);

            committee.Members = newMembers.ToList();
            _db.InnovationCommittees.Update(committee);

            await _db.CommitteeMembers.AddRangeAsync(newMembers, ct);
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