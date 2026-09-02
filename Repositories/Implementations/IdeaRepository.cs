using Ibtikar.Data;
using Ibtikar.DTOs.Ideas;
using Ibtikar.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Repositories
{
    public sealed class IdeaRepository : IIdeaRepository
    {
        private readonly IbtikarDbContext _db;

        public IdeaRepository(IbtikarDbContext db) => _db = db;

        public async Task<IReadOnlyList<IdeaSummaryDto>> GetLatestAsync(int take, CancellationToken ct)
        {
            return await _db.InnovationIdeas
                .AsNoTracking()
                .OrderByDescending(i => i.CreatedAt)
                .Take(take)
                .Select(i => new IdeaSummaryDto(
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    i.Title != null && i.Title.Length > 50
                        ? i.Title.Substring(0, 50) + "…"
                        : i.Title,
                    i.CurrentStatus != null ? i.CurrentStatus.Name : null,
                    i.CurrentStatus != null ? i.CurrentStatus.Color : null,
                    i.InnovationDomain != null ? i.InnovationDomain.Name : null,
                    i.ApplicantDepartment != null ? i.ApplicantDepartment.Name : null,
                    i.SubmittedAt,
                    i.CreatedAt))
                .ToListAsync(ct);
        }

        public async Task<IdeaDetailsDto?> GetDetailsAsync(string referenceNumber, Guid userId, CancellationToken ct)
        {
            return await _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => i.ReferenceNumber == referenceNumber && i.ApplicantUserId == userId)
                .Select(i => new IdeaDetailsDto(
                    i.ReferenceNumber,
                    i.Title,
                    i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    i.CurrentStatus != null ? i.CurrentStatus.Color : "#0d6efd",
                    i.InnovationDomain != null ? i.InnovationDomain.Name : "—",
                    i.SubmittedAt ?? i.CreatedAt))
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IdeaStatus?> GetStatusByCodeAsync(string code, CancellationToken ct)
        {
            return await _db.IdeaStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Code == code, ct);
        }

        public async Task<string> GenerateReferenceNumberAsync(CancellationToken ct)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"IBT-{year}-";
            var refs = await _db.InnovationIdeas
                .Where(i => i.ReferenceNumber.StartsWith(prefix))
                .Select(i => i.ReferenceNumber)
                .ToListAsync(ct);

            int maxSeq = 0;
            foreach (var refn in refs)
            {
                var tail = refn.Substring(prefix.Length);
                if (int.TryParse(tail, out var n) && n > maxSeq) maxSeq = n;
            }
            return $"{prefix}{(maxSeq + 1):D4}";
        }

        public async Task AddAsync(InnovationIdea idea, CancellationToken ct)
        {
            await _db.InnovationIdeas.AddAsync(idea, ct);
        }

        public async Task<InnovationIdea?> GetDraftByIdAsync(Guid ideaId, Guid applicantId, CancellationToken ct)
        {
            return await _db.InnovationIdeas
                .Include(i => i.Attachments)
                .FirstOrDefaultAsync(
                    i => i.Id == ideaId
                        && i.ApplicantUserId == applicantId
                        && i.IsDraft
                        && !i.IsDeleted,
                    ct);
        }

        public async Task<IReadOnlyList<Guid>> GetDraftTechnologyIdsAsync(Guid ideaId, CancellationToken ct)
        {
            return Array.Empty<Guid>();
        }

        public async Task SaveChangesAsync(CancellationToken ct)
        {
            await _db.SaveChangesAsync(ct);
        }

        public async Task<UserSummaryDto?> GetUserSummaryAsync(Guid userId, CancellationToken ct)
        {
            return await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new UserSummaryDto(
                    u.Id,
                    u.FullName,
                    u.Department != null ? u.Department.Name : null))
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IdeaLookupsDto> GetLookupsAsync(CancellationToken ct)
        {
            var domains = await GetActiveDomainsAsync(ct);
            var impacts = await GetActiveImpactsAsync(ct);
            var audiences = await GetActiveAudiencesAsync(ct);
            var technologies = await GetActiveTechnologiesAsync(ct);

            return new IdeaLookupsDto(domains, impacts, audiences, technologies);
        }

        private async Task<IReadOnlyList<SelectListItem>> GetActiveDomainsAsync(CancellationToken ct)
        {
            return await _db.InnovationDomains
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync(ct);
        }

        private async Task<IReadOnlyList<SelectListItem>> GetActiveImpactsAsync(CancellationToken ct)
        {
            return await _db.ExpectedImpacts
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync(ct);
        }

        private async Task<IReadOnlyList<SelectListItem>> GetActiveAudiencesAsync(CancellationToken ct)
        {
            return await _db.TargetAudiences
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync(ct);
        }

        private async Task<IReadOnlyList<SelectListItem>> GetActiveTechnologiesAsync(CancellationToken ct)
        {
            return await _db.Technologies
                .Where(t => t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync(ct);
        }
    }
}