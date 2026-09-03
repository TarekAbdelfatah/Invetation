using Ibtikar.DTOs.Committees;
using Ibtikar.Models;
using Ibtikar.Repositories;
using Ibtikar.Services.Interfaces;
using Ibtikar.Services.Notifications;

namespace Ibtikar.Services.Implementations
{
    public sealed class CommitteeFormationService : ICommitteeFormationService
    {
        private readonly ICommitteeRepository _committees;
        private readonly INotificationClient _notifier;
        private readonly ILogger<CommitteeFormationService> _logger;

        public CommitteeFormationService(
            ICommitteeRepository committees,
            INotificationClient notifier,
            ILogger<CommitteeFormationService> logger)
        {
            _committees = committees;
            _notifier = notifier;
            _logger = logger;
        }

        public Task<IReadOnlyList<CommitteeSummaryDto>> GetAllAsync(CancellationToken ct)
            => _committees.GetAllAsync(ct);

        public Task<CommitteeMemberOptionDto[]> GetMemberCandidatesAsync(Guid? excludeCommitteeId, CancellationToken ct)
            => _committees.GetMemberCandidatesAsync(excludeCommitteeId, ct);

        public async Task<CommitteeCreateResultDto> CreateAsync(Guid actorUserId, CommitteeCreateDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return new CommitteeCreateResultDto(false, "اسم اللجنة مطلوب.", null);
            }

            if (dto.HeadAdminId <= 0)
            {
                return new CommitteeCreateResultDto(false, "يجب اختيار رئيس واحد للجنة.", null);
            }

            var distinctMembers = (dto.MemberAdminIds ?? Array.Empty<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (distinctMembers.Count < 1)
            {
                return new CommitteeCreateResultDto(false, "يجب إضافة عضو واحد على الأقل للجنة.", null);
            }

            if (distinctMembers.Contains(dto.HeadAdminId))
            {
                return new CommitteeCreateResultDto(false, "لا يمكن اختيار رئيس اللجنة من ضمن الأعضاء؛ يُضاف الرئيس تلقائياً كعضو للجنة.", null);
            }

            var memberIds = distinctMembers
                .Append(dto.HeadAdminId)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var activeCommitteeMemberIds = await _committees.GetActiveCommitteeMemberIdsAsync(memberIds, ct);
            if (!activeCommitteeMemberIds.Contains(dto.HeadAdminId))
            {
                return new CommitteeCreateResultDto(false, "المستخدم المختار كرئيس ليس عضواً نشطاً في اللجان.", null);
            }

            if (activeCommitteeMemberIds.Count != memberIds.Count)
            {
                return new CommitteeCreateResultDto(false, "بعض الأعضاء المختارين ليسوا أعضاء نشطين في اللجان.", null);
            }

            var committee = new InnovationCommittee
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = actorUserId,
                Members = memberIds
                    .Select(adminId => new CommitteeMember
                    {
                        Id = Guid.NewGuid(),
                        AdminId = adminId,
                        IsHead = adminId == dto.HeadAdminId,
                        JoinedAt = DateTime.UtcNow
                    }).ToList()
            };

            await _committees.AddCommitteeAsync(committee, ct);

            _logger.LogInformation("Committee {Id} '{Name}' created with {Count} members by user {Actor}",
                committee.Id, committee.Name, committee.Members.Count, actorUserId);

            return new CommitteeCreateResultDto(true, "تم إنشاء اللجنة بنجاح.", committee.Id);
        }

        public async Task<CommitteeCreateResultDto> ActivateAsync(Guid actorUserId, Guid committeeId, CancellationToken ct)
        {
            var committee = await _committees.GetWithMembersAsync(committeeId, ct);

            if (committee is null)
            {
                return new CommitteeCreateResultDto(false, "اللجنة غير موجودة.", null);
            }

            if (committee.IsActive)
            {
                return new CommitteeCreateResultDto(false, "اللجنة مفعّلة مسبقًا.", null);
            }

            var members = committee.Members.Where(m => m.Admin is not null).ToList();
            if (members.Count < 1)
            {
                return new CommitteeCreateResultDto(false, "لا يمكن تفعيل لجنة بلا أعضاء.", null);
            }

            committee.IsActive = true;
            committee.ActivatedAt = DateTime.UtcNow;
            await _committees.SaveChangesAsync(ct);

            foreach (var member in members)
            {
                await SafeNotifyAsync("Committee.Activated", committee.Id.ToString(), new Dictionary<string, string>
                {
                    ["committeeId"] = committee.Id.ToString(),
                    ["committeeName"] = committee.Name,
                    ["memberAdminId"] = member.AdminId.ToString(),
                    ["memberName"] = member.Admin?.NetworkUser ?? string.Empty,
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