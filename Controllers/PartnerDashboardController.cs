using Ibtikar.DTOs.PartnerDashboard;
using Ibtikar.Models;
using Ibtikar.Services.Security;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ibtikar.Controllers
{
    [Authorize(Roles = RoleCodes.PartnerDepartment)]
    public class PartnerDashboardController : Controller
    {
        private readonly Services.PartnerDashboard.IPartnerDashboardService _service;
        private readonly ILogger<PartnerDashboardController> _logger;

        public PartnerDashboardController(
            Services.PartnerDashboard.IPartnerDashboardService service,
            ILogger<PartnerDashboardController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            try
            {
                var departmentId = ResolveDepartmentId();
                var snapshot = await _service.GetSnapshotAsync(departmentId, ct);
                var inbox = await _service.GetInboxAsync(departmentId, ct);

                var vm = new PartnerDashboardVm
                {
                    PendingAssignments = snapshot?.PendingAssignments ?? 0,
                    OverdueLate = snapshot?.OverdueLate ?? 0,
                    SubmittedThisCycle = snapshot?.SubmittedThisCycle ?? 0,
                    DepartmentName = ResolveDepartmentName(),
                    Items = (inbox?.Items ?? new List<PartnerAssignmentRowDto>())
                        .Select(ToListItemVm)
                        .ToList()
                };
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Partner dashboard index fallback (database unavailable): {Message}", ex.Message);
                ViewBag.DatabaseError = ex.Message;
                return View(new PartnerDashboardVm { Items = new List<PartnerAssignmentRowVm>() });
            }
        }

        [HttpGet("PartnerDashboard/Details/{assignmentId:guid}")]
        public async Task<IActionResult> Details(Guid assignmentId, CancellationToken ct)
        {
            try
            {
                var dto = await _service.GetDetailsAsync(ResolveDepartmentId(), assignmentId, ct);
                if (dto is null) return Forbid();

                return View(ToDetailsVm(dto));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Partner dashboard details fallback for {Assignment}: {Message}", assignmentId, ex.Message);
                return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
            }
        }

        [HttpPost("PartnerDashboard/Submit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(Guid assignmentId, string? comment, bool returnOnly, IFormCollection form, CancellationToken ct)
        {
            var scores = ParseScores(form);
            var submission = new PartnerSubmitDto(assignmentId, scores, comment, returnOnly);
            var result = await _service.SubmitAsync(ResolveDepartmentId(), ResolveUserId(), submission, ct);

            TempData[result.Success ? "AlertMessage" : "AlertError"] = result.Message ?? "حدث خطأ.";
            TempData["AlertType"] = result.Success ? "success" : "danger";
            return RedirectToAction(nameof(Details), new { assignmentId });
        }

        private static List<PartnerScoreInputDto> ParseScores(IFormCollection form)
        {
            var scores = new List<PartnerScoreInputDto>();
            foreach (var key in form.Keys.Where(k => k.StartsWith("score_")))
            {
                if (!Guid.TryParse(key.AsSpan(6), out var criterionId)) continue;
                if (!int.TryParse(form[key], out var score)) continue;
                var cKey = $"comment_{criterionId}";
                var c = form.TryGetValue(cKey, out var cv) ? cv.ToString() : null;
                scores.Add(new PartnerScoreInputDto(criterionId, score, c));
            }
            return scores;
        }

        private static PartnerAssignmentRowVm ToListItemVm(PartnerAssignmentRowDto i)
            => new(
                i.AssignmentId, i.IdeaId, i.IdeaReference, i.IdeaTitle,
                i.ApplicantName, i.SourceDepartmentName,
                i.SentAt, i.RespondedAt,
                i.Status, i.IsLate, i.IsPending, i.IsReturned, i.DaysOpen);

        private static PartnerDetailsVm ToDetailsVm(PartnerDetailsDto dto)
            => new()
            {
                AssignmentId = dto.AssignmentId,
                IdeaId = dto.IdeaId,
                IdeaReference = dto.IdeaReference,
                IdeaTitle = dto.IdeaTitle,
                Description = dto.Description,
                ProblemStatement = dto.ProblemStatement,
                ProposedSolution = dto.ProposedSolution,
                ExpectedBenefits = dto.ExpectedBenefits,
                DomainName = dto.DomainName,
                ApplicantName = dto.ApplicantName,
                ApplicantDepartmentName = dto.ApplicantDepartmentName,
                SourceDepartmentName = dto.SourceDepartmentName,
                Status = dto.Status,
                SentAt = dto.SentAt,
                RespondedAt = dto.RespondedAt,
                CanScore = dto.CanScore,
                AlreadyScored = dto.AlreadyScored,
                TotalScore = dto.TotalScore,
                Comment = dto.Comment,
                Criteria = dto.Criteria.Select(c => new PartnerCriterionVm(c.Id, c.Code, c.Name, c.DisplayOrder)).ToList(),
                ExistingScores = dto.ExistingScores.Select(s => new PartnerScoreLineVm(s.CriterionId, s.CriterionCode, s.CriterionName, s.Score, s.Comment)).ToList()
            };

        private Guid? ResolveDepartmentId()
        {
            var raw = User.FindFirst(RoleCodes.DepartmentIdClaim)?.Value;
            return Guid.TryParse(raw, out var id) ? id : null;
        }

        private string? ResolveDepartmentName()
            => User.FindFirst("ibtikar_department_name")?.Value;

        private Guid ResolveUserId()
        {
            var raw = User.FindFirst(RoleCodes.UserIdClaim)?.Value;
            return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
        }
    }
}