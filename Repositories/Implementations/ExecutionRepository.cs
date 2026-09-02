using Ibtikar.Data;
using Ibtikar.DTOs.Execution;
using Ibtikar.Models;
using Ibtikar.Services.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Repositories
{
    public sealed class ExecutionRepository : IExecutionRepository
    {
        private readonly IbtikarDbContext _db;

        public ExecutionRepository(IbtikarDbContext db) => _db = db;

        public async Task<ExecutionListDto> GetListAsync(Guid? departmentId, CancellationToken ct)
        {
            var accepted = await _db.IdeaStatuses
                .AsNoTracking()
                .Where(s => s.Code == IdeaStatusCodes.InExecution)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(ct);

            var query = _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => accepted != null && i.CurrentStatusId == accepted.Value);

            if (departmentId.HasValue)
                query = query.Where(i => i.AssignedDepartmentId == departmentId.Value);

            var rows = await query
                .OrderByDescending(i => i.AuditAssignedAt ?? i.CreatedAt)
                .Select(i => new
                {
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    DomainName = i.InnovationDomain != null ? i.InnovationDomain.Name : "—",
                    ApplicantName = i.ApplicantUser != null ? i.ApplicantUser.FullName : "—",
                    i.AssignedDepartmentId,
                    StatusName = i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    StatusColor = i.CurrentStatus != null ? i.CurrentStatus.Color : "#6c757d",
                    i.AuditAssignedAt,
                    i.CreatedAt,
                    i.IsDraft
                })
                .ToListAsync(ct);

            var ideaIds = rows.Select(r => r.Id).ToList();
            var latestStages = await _db.ExecutionProgresses
                .AsNoTracking()
                .Where(p => ideaIds.Contains(p.InnovationIdeaId))
                .GroupBy(p => p.InnovationIdeaId)
                .Select(g => new
                {
                    IdeaId = g.Key,
                    StageId = g.OrderByDescending(x => x.ChangedAt).Select(x => x.ExecutionStageId).FirstOrDefault(),
                    ChangedAt = g.Max(x => x.ChangedAt)
                })
                .ToListAsync(ct);

            var stageIds = latestStages.Select(s => s.StageId).Distinct().ToList();
            var stages = await _db.ExecutionStages
                .AsNoTracking()
                .Where(s => stageIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

            var items = rows.Select(r =>
            {
                var stageName = latestStages
                    .Where(s => s.IdeaId == r.Id)
                    .Select(s => stages.TryGetValue(s.StageId, out var n) ? n : (string?)null)
                    .FirstOrDefault();

                var canUpdate = !r.IsDraft
                    && (departmentId == null || r.AssignedDepartmentId == departmentId);
                var canComplete = canUpdate && stageName != null;

                return new ExecutionListItemDto(
                    r.Id,
                    r.ReferenceNumber,
                    r.Title,
                    r.DomainName,
                    r.ApplicantName,
                    r.AuditAssignedAt ?? r.CreatedAt,
                    stageName,
                    r.StatusName,
                    r.StatusColor,
                    canUpdate,
                    canComplete);
            }).ToList();

            var deptName = departmentId.HasValue
                ? await _db.Departments
                    .AsNoTracking()
                    .Where(d => d.Id == departmentId.Value)
                    .Select(d => d.Name)
                    .FirstOrDefaultAsync(ct) ?? string.Empty
                : string.Empty;

            return new ExecutionListDto(items, deptName);
        }

        public async Task<ExecutionHeaderDto?> GetHeaderAsync(Guid ideaId, Guid? departmentId, CancellationToken ct)
        {
            var idea = await _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => i.Id == ideaId)
                .Select(i => new
                {
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    DomainName = i.InnovationDomain != null ? i.InnovationDomain.Name : "—",
                    ApplicantName = i.ApplicantUser != null ? i.ApplicantUser.FullName : "—",
                    ApplicantDeptName = i.ApplicantDepartment != null ? i.ApplicantDepartment.Name : "خارجي",
                    AssignedDeptName = i.AssignedDepartment != null ? i.AssignedDepartment.Name : "—",
                    i.AssignedDepartmentId,
                    StatusName = i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    StatusColor = i.CurrentStatus != null ? i.CurrentStatus.Color : "#6c757d",
                    i.IsDraft
                })
                .FirstOrDefaultAsync(ct);
            if (idea is null) return null;

            var currentStage = await _db.ExecutionProgresses
                .AsNoTracking()
                .Where(p => p.InnovationIdeaId == ideaId)
                .OrderByDescending(p => p.ChangedAt)
                .Select(p => new ExecutionStageDto(p.ExecutionStageId, 0, string.Empty, string.Empty))
                .FirstOrDefaultAsync(ct);

            ExecutionStageDto? currentStageDto = null;
            if (currentStage is not null)
            {
                var s = await _db.ExecutionStages.AsNoTracking()
                    .Where(x => x.Id == currentStage.Id)
                    .Select(x => new { x.Id, x.Order, x.Code, x.Name })
                    .FirstOrDefaultAsync(ct);
                if (s is not null) currentStageDto = new ExecutionStageDto(s.Id, s.Order, s.Code, s.Name);
            }

            var stages = await _db.ExecutionStages
                .AsNoTracking()
                .OrderBy(s => s.Order)
                .Select(s => new ExecutionStageDto(s.Id, s.Order, s.Code, s.Name))
                .ToListAsync(ct);

            var canUpdate = !idea.IsDraft
                && (departmentId == null || idea.AssignedDepartmentId == departmentId);
            var canComplete = canUpdate && currentStageDto is not null;

            return new ExecutionHeaderDto(
                idea.Id,
                idea.ReferenceNumber,
                idea.Title,
                idea.DomainName,
                idea.ApplicantName,
                idea.ApplicantDeptName,
                idea.AssignedDeptName,
                idea.StatusName,
                idea.StatusColor,
                stages,
                currentStageDto,
                canUpdate,
                canComplete);
        }

        public async Task<ExecutionTimelineDto?> GetTimelineAsync(Guid ideaId, Guid? departmentId, CancellationToken ct)
        {
            var header = await _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => i.Id == ideaId)
                .Select(i => new { i.Id, i.ReferenceNumber, i.Title, i.AssignedDepartmentId, i.IsDraft })
                .FirstOrDefaultAsync(ct);
            if (header is null) return null;

            if (departmentId.HasValue && header.AssignedDepartmentId != departmentId.Value)
                return null;

            var rows = await _db.ExecutionProgresses
                .AsNoTracking()
                .Where(p => p.InnovationIdeaId == ideaId)
                .OrderByDescending(p => p.ChangedAt)
                .Select(p => new
                {
                    p.ChangedAt,
                    p.Note,
                    StageName = p.ExecutionStage != null ? p.ExecutionStage.Name : "—",
                    StageOrder = p.ExecutionStage != null ? p.ExecutionStage.Order : 0,
                    ChangedByName = p.ChangedBy != null ? p.ChangedBy.FullName : null
                })
                .ToListAsync(ct);

            return new ExecutionTimelineDto(
                header.Id,
                header.ReferenceNumber,
                header.Title,
                rows.Select(r => new ExecutionTimelineRowDto(
                    r.ChangedAt,
                    r.StageName,
                    r.StageOrder,
                    r.ChangedByName,
                    r.Note)).ToList());
        }

        public async Task<bool> IsAssigneeAsync(Guid ideaId, Guid? departmentId, CancellationToken ct)
        {
            if (!departmentId.HasValue) return true;
            return await _db.InnovationIdeas
                .AsNoTracking()
                .AnyAsync(i => i.Id == ideaId && i.AssignedDepartmentId == departmentId.Value, ct);
        }

        public async Task<Guid?> GetCompletedStatusIdAsync(CancellationToken ct)
            => await _db.IdeaStatuses
                .AsNoTracking()
                .Where(s => s.Code == IdeaStatusCodes.Completed)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(ct);

        public async Task<InnovationIdea?> GetIdeaWithStatusAsync(Guid ideaId, CancellationToken ct)
            => await _db.InnovationIdeas
                .Include(i => i.CurrentStatus)
                .FirstOrDefaultAsync(i => i.Id == ideaId, ct);

        public async Task<ExecutionStage?> GetActiveStageByIdAsync(Guid stageId, CancellationToken ct)
            => await _db.ExecutionStages
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == stageId && s.IsActive, ct);

        public async Task AddProgressAsync(ExecutionProgress progress, CancellationToken ct)
        {
            await _db.ExecutionProgresses.AddAsync(progress, ct);
            await _db.SaveChangesAsync(ct);
        }

        public async Task AddProgressAndStatusAsync(ExecutionProgress progress, IdeaStatusHistory history, CancellationToken ct)
        {
            await _db.ExecutionProgresses.AddAsync(progress, ct);
            await _db.IdeaStatusHistories.AddAsync(history, ct);
            await _db.SaveChangesAsync(ct);
        }

        public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
    }
}
