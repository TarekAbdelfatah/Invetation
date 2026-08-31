using Ibtikar.Data;
using Ibtikar.DTOs.Committees;
using Ibtikar.Models;
using Ibtikar.Services.Interfaces;
using Ibtikar.Services.Notifications;
using Ibtikar.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Services.Implementations
{
    public sealed class CommitteeFormationService : ICommitteeFormationService
    {
        private readonly IbtikarDbContext _db;
        private readonly INotificationClient _notifier;
        private readonly ILogger<CommitteeFormationService> _logger;

        public CommitteeFormationService(
            IbtikarDbContext db,
            INotificationClient notifier,
            ILogger<CommitteeFormationService> logger)
        {
            _db = db;
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<IReadOnlyList<CommitteeSummaryDto>> GetAllAsync(CancellationToken ct)
        {
            var committees = await _db.InnovationCommittees.AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(ct);

            var memberLookup = await _db.CommitteeMembers.AsNoTracking()
                .Where(m => committees.Select(c => c.Id).Contains(m.InnovationCommitteeId))
                .GroupBy(m => m.InnovationCommitteeId)
                .Select(g => new { CommitteeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CommitteeId, x => x.Count, ct);

            var headLookup = await _db.Users.AsNoTracking()
                .Where(u => _db.CommitteeMembers.Any(m => m.UserId == u.Id && m.IsHead))
                .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

            return committees.Select(c =>
            {
                var headUserId = _db.CommitteeMembers.AsNoTracking()
                    .Where(m => m.InnovationCommitteeId == c.Id && m.IsHead)
                    .Select(m => m.UserId)
                    .FirstOrDefault();
                var headName = headUserId != Guid.Empty && headLookup.TryGetValue(headUserId, out var n)
                    ? n
                    : "—";
                var count = memberLookup.TryGetValue(c.Id, out var k) ? k : 0;
                return new CommitteeSummaryDto(c.Id, c.Name, c.Description, c.IsActive, c.CreatedAt, c.ActivatedAt, headName, count);
            }).ToList();
        }

        public async Task<CommitteeMemberOptionDto[]> GetMemberCandidatesAsync(Guid? excludeCommitteeId, CancellationToken ct)
        {
            var roleCode = RoleCodes.InnovationCommitteeMember;
            var users = await _db.UserRoles.AsNoTracking()
                .Where(ur => ur.Role.Code == roleCode && ur.User.IsActive)
                .Select(ur => new { ur.UserId, ur.User.FullName, ur.User.Username, ur.User.Id })
                .ToListAsync(ct);

            var alreadyOnCommittee = await _db.CommitteeMembers.AsNoTracking()
                .Where(m => excludeCommitteeId == null || m.InnovationCommitteeId == excludeCommitteeId)
                .Select(m => m.UserId)
                .ToListAsync(ct);

            var alreadySet = new HashSet<Guid>(alreadyOnCommittee);

            return users
                .Where(u => !alreadySet.Contains(u.Id))
                .Select(u => new CommitteeMemberOptionDto(u.Id, u.FullName, u.Username, false))
                .OrderBy(u => u.FullName)
                .ToArray();
        }

        public async Task<CommitteeCreateResultDto> CreateAsync(Guid actorUserId, CommitteeCreateDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return new CommitteeCreateResultDto(false, "اسم اللجنة مطلوب.", null);
            }

            if (dto.HeadUserId == Guid.Empty)
            {
                return new CommitteeCreateResultDto(false, "يجب اختيار رئيس واحد للجنة.", null);
            }

            var distinctMembers = (dto.MemberUserIds ?? Array.Empty<Guid>())
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            if (distinctMembers.Count < 1)
            {
                return new CommitteeCreateResultDto(false, "يجب إضافة عضو واحد على الأقل للجنة.", null);
            }

            if (!distinctMembers.Contains(dto.HeadUserId))
            {
                return new CommitteeCreateResultDto(false, "يجب أن يكون الرئيس ضمن قائمة الأعضاء.", null);
            }

            if (distinctMembers.Count != distinctMembers.Distinct().Count())
            {
                return new CommitteeCreateResultDto(false, "لا يمكن تكرار نفس العضو.", null);
            }

            if (distinctMembers.Count(m => m == dto.HeadUserId) != 1)
            {
                return new CommitteeCreateResultDto(false, "يجب وجود رئيس واحد فقط للجنة.", null);
            }

            var headUser = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == dto.HeadUserId, ct);
            if (headUser is null || !headUser.IsActive)
            {
                return new CommitteeCreateResultDto(false, "المستخدم المختار كرئيس غير موجود أو غير فعال.", null);
            }

            var memberUsers = await _db.Users.AsNoTracking()
                .Where(u => distinctMembers.Contains(u.Id) && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync(ct);
            if (memberUsers.Count != distinctMembers.Count)
            {
                return new CommitteeCreateResultDto(false, "بعض الأعضاء المختارين غير موجودين أو غير فعالين.", null);
            }

            var committee = new InnovationCommittee
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = actorUserId,
                Members = distinctMembers.Select(uid => new CommitteeMember
                {
                    Id = Guid.NewGuid(),
                    UserId = uid,
                    IsHead = uid == dto.HeadUserId,
                    JoinedAt = DateTime.UtcNow
                }).ToList()
            };

            _db.InnovationCommittees.Add(committee);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Committee {Id} '{Name}' created with {Count} members by user {Actor}",
                committee.Id, committee.Name, committee.Members.Count, actorUserId);

            return new CommitteeCreateResultDto(true, "تم إنشاء اللجنة بنجاح.", committee.Id);
        }

        public async Task<CommitteeCreateResultDto> ActivateAsync(Guid actorUserId, Guid committeeId, CancellationToken ct)
        {
            var committee = await _db.InnovationCommittees
                .Include(c => c.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(c => c.Id == committeeId, ct);

            if (committee is null)
            {
                return new CommitteeCreateResultDto(false, "اللجنة غير موجودة.", null);
            }

            if (committee.IsActive)
            {
                return new CommitteeCreateResultDto(false, "اللجنة مفعّلة مسبقًا.", null);
            }

            var members = committee.Members.Where(m => m.User is not null).ToList();
            if (members.Count < 1)
            {
                return new CommitteeCreateResultDto(false, "لا يمكن تفعيل لجنة بلا أعضاء.", null);
            }

            committee.IsActive = true;
            committee.ActivatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            foreach (var member in members)
            {
                await SafeNotifyAsync("Committee.Activated", committee.Id.ToString(), new Dictionary<string, string>
                {
                    ["committeeId"] = committee.Id.ToString(),
                    ["committeeName"] = committee.Name,
                    ["memberUserId"] = member.UserId.ToString(),
                    ["memberName"] = member.User?.FullName ?? string.Empty,
                    ["isHead"] = member.IsHead ? "true" : "false",
                    ["activatedAt"] = DateTime.UtcNow.ToString("O")
                }, ct);
            }

            _logger.LogInformation("Committee {Id} '{Name}' activated by user {Actor} with {Count} members notified",
                committee.Id, committee.Name, actorUserId, members.Count);

            return new CommitteeCreateResultDto(true, "تم تفعيل اللجنة وإشعار الأعضاء.", committee.Id);
        }

        private async Task SafeNotifyAsync(string action, string entityId, IDictionary<string, string>? payload, CancellationToken ct)
        {
            try
            {
                await _notifier.SendAsync(action, entityId, payload, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // request cancelled; activation already committed and must not roll back.
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Notify {Action} failed for {Entity}", action, entityId);
            }
        }
    }
}
