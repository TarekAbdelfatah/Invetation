using Ibtikar.Data;
using Ibtikar.Models;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Repositories
{
    public sealed class IdeaRepository : IIdeaRepository
    {
        private readonly IbtikarDbContext _db;

        public IdeaRepository(IbtikarDbContext db) => _db = db;

        public async Task<IReadOnlyList<InnovationIdea>> GetLatestAsync(int take, CancellationToken ct)
        {
            return await _db.InnovationIdeas
                .AsNoTracking()
                .Include(i => i.CurrentStatus)
                .Include(i => i.InnovationDomain)
                .Include(i => i.ApplicantDepartment)
                .OrderByDescending(i => i.CreatedAt)
                .Take(take)
                .ToListAsync(ct);
        }

        public async Task<InnovationIdea?> GetByReferenceForUserAsync(string referenceNumber, Guid userId, CancellationToken ct)
        {
            return await _db.InnovationIdeas
                .AsNoTracking()
                .Include(i => i.CurrentStatus)
                .Include(i => i.InnovationDomain)
                .Where(i => i.ReferenceNumber == referenceNumber && i.ApplicantUserId == userId)
                .Select(i => new InnovationIdea
                {
                    Id = i.Id,
                    ReferenceNumber = i.ReferenceNumber,
                    Title = i.Title,
                    CurrentStatus = i.CurrentStatus,
                    InnovationDomain = i.InnovationDomain,
                    SubmittedAt = i.SubmittedAt,
                    CreatedAt = i.CreatedAt
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IdeaSuccessVm?> GetSuccessVmByReferenceAsync(string referenceNumber, Guid userId, CancellationToken ct)
        {
            return await _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => i.ReferenceNumber == referenceNumber && i.ApplicantUserId == userId)
                .Select(i => new IdeaSuccessVm(
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

        public async Task SaveChangesAsync(CancellationToken ct)
        {
            await _db.SaveChangesAsync(ct);
        }

        public async Task<User?> GetUserWithDepartmentAsync(Guid userId, CancellationToken ct)
        {
            return await _db.Users
                .AsNoTracking()
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == userId, ct);
        }

        public async Task<IReadOnlyList<SelectListItem>> GetActiveDomainsAsync(CancellationToken ct)
        {
            return await _db.InnovationDomains
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<SelectListItem>> GetActiveImpactsAsync(CancellationToken ct)
        {
            return await _db.ExpectedImpacts
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<SelectListItem>> GetActiveAudiencesAsync(CancellationToken ct)
        {
            return await _db.TargetAudiences
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<SelectListItem>> GetActiveTechnologiesAsync(CancellationToken ct)
        {
            return await _db.Technologies
                .Where(t => t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync(ct);
        }
    }
}
