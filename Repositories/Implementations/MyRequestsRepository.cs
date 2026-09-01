using Ibtikar.Data;
using Ibtikar.DTOs.MyRequests;
using Ibtikar.Models;
using Ibtikar.Services.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Repositories
{
    public sealed class MyRequestsRepository : IMyRequestsRepository
    {
        private readonly IbtikarDbContext _db;

        public MyRequestsRepository(IbtikarDbContext db) => _db = db;

        public async Task<MyRequestsListDto> GetListAsync(Guid applicantId, int page, int pageSize, CancellationToken ct)
        {
            var baseQuery = _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => i.ApplicantUserId == applicantId && !i.IsDeleted);

            var totalCount = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new MyRequestSummaryDto(
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    i.IsDraft,
                    i.IsDraft
                        ? string.Empty
                        : (i.CurrentStatus != null ? i.CurrentStatus.Code : string.Empty),
                    i.IsDraft
                        ? "مسودة"
                        : (i.CurrentStatus != null ? i.CurrentStatus.Name : "—"),
                    i.IsDraft
                        ? "#6c757d"
                        : (i.CurrentStatus != null ? i.CurrentStatus.Color : "#888"),
                    i.CreatedAt,
                    i.SubmittedAt))
                .ToListAsync(ct);

            return new MyRequestsListDto(items, page, pageSize, totalCount);
        }

        public async Task<MyRequestDetailsDto?> GetDetailsAsync(Guid applicantId, Guid id, CancellationToken ct)
        {
            var header = await _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => i.Id == id && i.ApplicantUserId == applicantId && !i.IsDeleted)
                .Select(i => new DetailsHeader(
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    i.Description,
                    i.ProblemStatement,
                    i.ProposedSolution,
                    i.ExpectedBenefits,
                    i.ExpectedImpactOther,
                    i.TargetAudienceOther,
                    i.UsesEmergingTech,
                    i.TechnologyOther,
                    i.CurrentStatus != null ? i.CurrentStatus.Code : string.Empty,
                    i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    i.CurrentStatus != null ? i.CurrentStatus.Color : "#6c757d",
                    i.InnovationDomain != null ? i.InnovationDomain.Name : null,
                    i.ExpectedImpact != null ? i.ExpectedImpact.Name : null,
                    i.TargetAudience != null ? i.TargetAudience.Name : null,
                    i.CreatedAt,
                    i.SubmittedAt))
                .FirstOrDefaultAsync(ct);

            if (header is null) return null;

            var attachments = await _db.IdeaAttachments
                .AsNoTracking()
                .Where(a => a.InnovationIdeaId == id)
                .OrderBy(a => a.UploadedAt)
                .Select(a => new MyRequestAttachmentDto(a.Id, a.FileName, a.SizeBytes, a.UploadedAt))
                .ToListAsync(ct);

            var history = await _db.IdeaStatusHistories
                .AsNoTracking()
                .Where(h => h.InnovationIdeaId == id)
                .Select(h => new HistoryEntry(
                    h.ToStatus != null ? h.ToStatus.Code : string.Empty,
                    h.ChangedAt,
                    h.Note))
                .ToListAsync(ct);

            return new MyRequestDetailsDto(
                header.Id,
                header.Reference,
                header.Title,
                header.Description,
                header.ProblemStatement,
                header.ProposedSolution,
                header.ExpectedBenefits,
                header.ExpectedImpactOther,
                header.TargetAudienceOther,
                header.UsesEmergingTech,
                header.TechnologyOther,
                header.StatusCode,
                header.StatusName,
                header.StatusColor,
                header.DomainName,
                header.ExpectedImpactName,
                header.TargetAudienceName,
                header.CreatedAt,
                header.SubmittedAt,
                header.StatusCode == IdeaStatusCodes.WaitingForCompletion
                    ? LatestNoteFor(history, IdeaStatusCodes.WaitingForCompletion) : null,
                header.StatusCode == IdeaStatusCodes.ReturnedForDevelopment
                    ? LatestNoteFor(history, IdeaStatusCodes.ReturnedForDevelopment) : null,
                header.StatusCode == IdeaStatusCodes.Rejected
                    ? LatestNoteFor(history, IdeaStatusCodes.Rejected) : null,
                attachments);
        }

        public async Task<InnovationIdea?> GetForApplicantAsync(Guid applicantId, Guid id, CancellationToken ct)
        {
            return await _db.InnovationIdeas
                .Include(i => i.CurrentStatus)
                .Include(i => i.Attachments)
                .FirstOrDefaultAsync(i => i.Id == id && i.ApplicantUserId == applicantId && !i.IsDeleted, ct);
        }

        public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

        public async Task<Guid?> GetStatusIdByCodeAsync(string code, CancellationToken ct)
            => await _db.IdeaStatuses
                .AsNoTracking()
                .Where(s => s.Code == code)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(ct);

        private static string? LatestNoteFor(IReadOnlyList<HistoryEntry> history, string statusCode)
            => history
                .Where(h => string.Equals(h.StatusCode, statusCode, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => h.Note)
                .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));

        private sealed record DetailsHeader(
            Guid Id,
            string Reference,
            string Title,
            string Description,
            string? ProblemStatement,
            string? ProposedSolution,
            string? ExpectedBenefits,
            string? ExpectedImpactOther,
            string? TargetAudienceOther,
            bool UsesEmergingTech,
            string? TechnologyOther,
            string StatusCode,
            string StatusName,
            string StatusColor,
            string? DomainName,
            string? ExpectedImpactName,
            string? TargetAudienceName,
            DateTime CreatedAt,
            DateTime? SubmittedAt);

        private sealed record HistoryEntry(string StatusCode, DateTime ChangedAt, string? Note);
    }
}