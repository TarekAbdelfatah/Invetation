using Ibtikar.DTOs.AdminOverview;
using Ibtikar.Services.Admin;
using Ibtikar.Services.Helpers;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ibtikar.Controllers
{
    [Authorize(Roles = RoleCodes.SystemAdmin)]
    public class AdminOverviewController : Controller
    {
        private readonly IAdminOverviewService _service;

        public AdminOverviewController(IAdminOverviewService service) => _service = service;

        public async Task<IActionResult> Index(string? status, CancellationToken ct)
        {
            var snapshot = await _service.GetSnapshotAsync(ct);
            var ideas = await _service.GetIdeasAsync(status, 200, ct);

            return View(ToVm(snapshot, ideas));
        }

        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var dto = await _service.GetDetailsAsync(id, ct);
            if (dto is null) return NotFound();

            var vm = new AdminOverviewDetailsVm
            {
                Id = dto.Id,
                Reference = dto.Reference,
                Title = dto.Title,
                Description = dto.Description,
                ProblemStatement = dto.ProblemStatement,
                ProposedSolution = dto.ProposedSolution,
                ExpectedBenefits = dto.ExpectedBenefits,
                DomainName = dto.DomainName,
                ExpectedImpactName = dto.ExpectedImpactName,
                TargetAudienceName = dto.TargetAudienceName,
                ApplicantName = dto.ApplicantName,
                ApplicantDepartmentName = dto.ApplicantDepartmentName,
                AssignedDepartmentName = dto.AssignedDepartmentName,
                StatusName = dto.StatusName,
                StatusColor = dto.StatusColor,
                StatusCode = dto.StatusCode,
                IsDraft = dto.IsDraft,
                SubmittedAt = dto.SubmittedAt,
                CreatedAt = dto.CreatedAt,
                Attachments = dto.Attachments
                    .Select(a => new AdminOverviewAttachmentVm(a.Id, a.FileName, a.SizeBytes, a.UploadedAt, a.UploadedByName))
                    .ToList(),
                Assessments = dto.Assessments
                    .Select(a => new AdminOverviewAssessmentVm(
                        a.Id, a.Source, a.SourceLabel, a.AssessorName, a.DepartmentName,
                        a.IsDraft, a.IsLocked, a.SubmittedAt, a.TotalScore, a.Comment,
                        a.Lines.Select(l => new AdminOverviewAssessmentLineVm(
                            l.CriterionId, l.CriterionCode, l.CriterionName, l.Score, l.Comment)).ToList()))
                    .ToList(),
                Timeline = dto.Timeline
                    .Select(t => new AdminOverviewTimelineRowVm(t.ChangedAt, t.FromStatus, t.ToStatus, t.By, t.Note))
                    .ToList()
            };
            return View(vm);
        }

        private static AdminOverviewVm ToVm(AdminOverviewDto dto, AdminOverviewListDto ideas)
            => new()
            {
                TotalIdeas = dto.TotalIdeas,
                Drafts = dto.Drafts,
                Submitted = dto.Submitted,
                TotalUsers = dto.TotalUsers,
                ByStatus = dto.ByStatus
                    .Select(s => new AdminOverviewVm.StatusCount(s.Code, s.Name, s.Color, s.Count))
                    .ToList(),
                Recent = dto.Recent
                    .Select(r => new AdminOverviewVm.RecentIdea(
                        r.Reference,
                        r.Title,
                        r.StatusName,
                        r.StatusColor,
                        r.Domain,
                        r.CreatedAt))
                    .ToList(),
                StatusFilter = ideas.StatusFilter,
                Ideas = ideas.Rows.Select(i => new AdminOverviewVm.IdeaRow(
                    i.Id, i.Reference, i.Title, i.DomainName, i.ApplicantName,
                    i.ApplicantDepartmentName, i.AssignedDepartmentName,
                    i.StatusCode, i.StatusName, i.StatusColor, i.CreatedAt, i.IsDraft)).ToList(),
                IdeasTotalCount = ideas.TotalCount
            };
    }
}