using Ibtikar.DTOs.Reports;
using Ibtikar.Services;
using Ibtikar.Services.Helpers;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ibtikar.Controllers
{
    [Authorize(Roles = RoleCodes.SystemAdmin)]
    public class ReportsController : Controller
    {
        private const string EmptyRangeMessage = "لا توجد بيانات في الفترة المختارة.";

        private readonly IReportsService _service;

        public ReportsController(IReportsService service) => _service = service;

        [HttpGet("Reports")]
        public async Task<IActionResult> Index(DateTime? from, DateTime? to, CancellationToken ct)
        {
            var (fromUtc, toUtc, error) = ResolveRange(from, to);
            if (error is not null)
            {
                TempData["AlertError"] = error;
                return View(BuildEmptyVm(fromUtc, toUtc));
            }

            var summary = await _service.GetSummaryAsync(fromUtc, toUtc, ct);
            return View(ToVm(summary));
        }

        [HttpGet("Reports/Challenges")]
        public async Task<IActionResult> Challenges(DateTime? from, DateTime? to, Guid? domainId, int take = 200, CancellationToken ct = default)
        {
            var (fromUtc, toUtc, error) = ResolveRange(from, to);
            if (error is not null)
            {
                TempData["AlertError"] = error;
            }

            var takeClamped = Math.Clamp(take, 10, 500);
            var dto = await _service.GetChallengesAsync(fromUtc, toUtc, domainId, takeClamped, ct);

            return View(new ReportsChallengesVm
            {
                From = fromUtc,
                To = toUtc.AddDays(-1),
                DomainId = dto.DomainId,
                Domains = dto.Domains.Select(d => new ReportsDomainOptionVm(d.Id, d.Name)).ToList(),
                Rows = dto.Rows.Select(r => new ReportsChallengeRowVm(
                    r.IdeaId, r.Reference, r.Title, r.DomainName, r.ApplicantName, r.ApplicantDepartmentName,
                    r.ProblemStatement, r.ProposedSolution, r.CreatedAt, r.StatusName, r.StatusColor)).ToList(),
                TotalCount = dto.TotalCount
            });
        }

        private static (DateTime From, DateTime To, string? Error) ResolveRange(DateTime? from, DateTime? to)
        {
            var today = DateTime.UtcNow.Date;
            var fromDefault = today.AddDays(-29);
            var toDefault = today;

            var fromValue = (from ?? fromDefault).Date;
            var toValue = (to ?? toDefault).Date;

            if (fromValue > toValue)
                return (fromValue, toValue.AddDays(1), "تاريخ البداية بعد تاريخ النهاية.");

            return (fromValue, toValue.AddDays(1), null);
        }

        private static ReportsVm BuildEmptyVm(DateTime from, DateTime to)
            => new()
            {
                From = from,
                To = to.AddDays(-1),
                ShowValidation = true,
                ValidationMessage = EmptyRangeMessage
            };

        private static ReportsVm ToVm(ReportsSummaryDto dto)
            => new()
            {
                From = dto.Range.From,
                To = dto.Range.To.AddDays(-1),
                Kpis = new ReportsKpiVm
                {
                    TotalIdeas = dto.Kpis.TotalIdeas,
                    Submitted = dto.Kpis.Submitted,
                    Approved = dto.Kpis.Approved,
                    InExecution = dto.Kpis.InExecution,
                    Completed = dto.Kpis.Completed
                },
                StageMix = dto.StageMix
                    .Select(s => new ReportsStageMixRowVm(s.Code, s.Name, s.Color, s.Count, s.Percent))
                    .ToList(),
                Warning = dto.Warning,
                ShowValidation = dto.IsEmpty,
                ValidationMessage = dto.IsEmpty ? EmptyRangeMessage : null
            };
    }
}
