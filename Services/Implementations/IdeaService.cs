using Ibtikar.Models;
using Ibtikar.Repositories;
using Ibtikar.Services.Attachments;
using Ibtikar.Services.Integrations;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Http;

namespace Ibtikar.Services.Ideas
{
    public sealed class IdeaService : IIdeaService
    {
        private readonly IIdeaRepository _repo;
        private readonly AttachmentService _attachments;
        private readonly ProcedureGatewayService _procedureGateway;
        private readonly ILogger<IdeaService> _logger;

        public IdeaService(
            IIdeaRepository repo,
            AttachmentService attachments,
            ProcedureGatewayService procedureGateway,
            ILogger<IdeaService> logger)
        {
            _repo = repo;
            _attachments = attachments;
            _procedureGateway = procedureGateway;
            _logger = logger;
        }

        public async Task<IdeaCreateOutcome> CreateIdeaAsync(
            IdeaCreateViewModel model,
            Guid userId,
            Guid? departmentId,
            bool isSaveDraft,
            List<IFormFile>? attachments,
            CancellationToken ct)
        {
            var idea = BuildIdea(model, userId, departmentId, isSaveDraft);

            if (!isSaveDraft)
            {
                idea.ReferenceNumber = await _repo.GenerateReferenceNumberAsync(ct);
                var newStatus = await _repo.GetStatusByCodeAsync(IdeaStatusCodes.New, ct);
                if (newStatus is null)
                {
                    _logger.LogError("IdeaStatus with code 'new' is missing from seed.");
                    return IdeaCreateOutcome.Failed("حالة الطلب غير مهيأة. تواصل مع مدير النظام.");
                }
                idea.CurrentStatusId = newStatus.Id;
            }
            else
            {
                idea.ReferenceNumber = string.Empty;
                var draftStatus = await _repo.GetStatusByCodeAsync(IdeaStatusCodes.New, ct);
                idea.CurrentStatusId = draftStatus?.Id ?? Guid.Empty;
            }

            await _repo.AddAsync(idea, ct);

            try
            {
                await _repo.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist idea for user {UserId}", userId);
                return IdeaCreateOutcome.Failed("تعذّر حفظ الطلب. حاول مرة أخرى.");
            }

            if (!isSaveDraft && !string.IsNullOrEmpty(idea.ReferenceNumber))
            {
                var attachError = await SaveAttachmentsAsync(idea.Id, userId, attachments, ct);
                if (attachError is not null)
                {
                    return IdeaCreateOutcome.Failed(attachError);
                }

                try
                {
                    await _procedureGateway.NotifyAsync(idea.ReferenceNumber, ct);
                }
                catch (Exception notifyEx)
                {
                    _logger.LogWarning(notifyEx,
                        "Procedure notify failed for {Reference} but idea is persisted.",
                        idea.ReferenceNumber);
                }

                return IdeaCreateOutcome.Submitted(idea.ReferenceNumber);
            }

            return IdeaCreateOutcome.DraftSaved();
        }

        public async Task<InnovationIdea?> GetByReferenceForUserAsync(string referenceNumber, Guid userId, CancellationToken ct)
            => await _repo.GetByReferenceForUserAsync(referenceNumber, userId, ct);

        public async Task<IdeaSuccessVm?> GetSuccessVmByReferenceAsync(string referenceNumber, Guid userId, CancellationToken ct)
            => await _repo.GetSuccessVmByReferenceAsync(referenceNumber, userId, ct);

        public async Task<IReadOnlyList<InnovationIdea>> GetLatestAsync(int take, CancellationToken ct)
            => await _repo.GetLatestAsync(take, ct);

        public async Task<IdeaLookups> GetLookupsAsync(CancellationToken ct)
        {
            var domains = await _repo.GetActiveDomainsAsync(ct);
            var impacts = await _repo.GetActiveImpactsAsync(ct);
            var audiences = await _repo.GetActiveAudiencesAsync(ct);
            var technologies = await _repo.GetActiveTechnologiesAsync(ct);

            return new IdeaLookups(domains, impacts, audiences, technologies);
        }

        public async Task<User?> GetUserWithDepartmentAsync(Guid userId, CancellationToken ct)
            => await _repo.GetUserWithDepartmentAsync(userId, ct);

        private static InnovationIdea BuildIdea(
            IdeaCreateViewModel model,
            Guid userId,
            Guid? departmentId,
            bool isSaveDraft)
        {
            return new InnovationIdea
            {
                Id = Guid.NewGuid(),
                Title = model.Title.Trim(),
                Description = model.Description.Trim(),
                ProblemStatement = NullIfBlank(model.ProblemStatement),
                ProposedSolution = NullIfBlank(model.ProposedSolution),
                ExpectedBenefits = NullIfBlank(model.ExpectedBenefits),
                ExpectedImpactOther = NullIfBlank(model.ExpectedImpactOther),
                TargetAudienceOther = NullIfBlank(model.TargetAudienceOther),
                UsesEmergingTech = model.UsesEmergingTech,
                TechnologyOther = NullIfBlank(model.TechnologyOther),
                InnovationDomainId = model.InnovationDomainId ?? Guid.Empty,
                ExpectedImpactId = model.ExpectedImpactId,
                TargetAudienceId = model.TargetAudienceId,
                ApplicantUserId = userId,
                ApplicantDepartmentId = departmentId,
                IsDraft = isSaveDraft,
                CreatedAt = DateTime.UtcNow,
                SubmittedAt = isSaveDraft ? null : DateTime.UtcNow
            };
        }

        private async Task<string?> SaveAttachmentsAsync(
            Guid ideaId,
            Guid userId,
            List<IFormFile>? attachments,
            CancellationToken ct)
        {
            if (attachments is null || attachments.Count == 0) return null;
            foreach (var file in attachments)
            {
                if (file is null || file.Length == 0) continue;
                var result = await _attachments.SaveAsync(ideaId, userId, file, ct);
                if (!result.Success)
                {
                    return result.Error;
                }
            }
            return null;
        }

        private static string? NullIfBlank(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
