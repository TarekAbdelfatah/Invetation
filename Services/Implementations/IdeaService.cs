using Ibtikar.DTOs.Ideas;
using Ibtikar.Models;
using Ibtikar.Repositories;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Ibtikar.Services.Implementations
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
            CreateIdeaRequestDto request,
            Guid userId,
            Guid? departmentId,
            bool isSaveDraft,
            Guid? draftId,
            Guid? existingDraftId,
            List<IFormFile>? attachments,
            CancellationToken ct)
        {
            InnovationIdea idea;
            var isUpdate = existingDraftId is { } && existingDraftId.Value != Guid.Empty;

            if (isUpdate)
            {
                var existing = await _repo.GetDraftByIdAsync(existingDraftId!.Value, userId, ct);
                if (existing is null)
                {
                    return IdeaCreateOutcome.Failed("تعذّر العثور على المسودة المطلوب تحديثها.");
                }
                idea = existing;
                ApplyToIdea(idea, request, departmentId, isSaveDraft);
            }
            else
            {
                idea = BuildIdea(request, userId, departmentId, isSaveDraft);
                await _repo.AddAsync(idea, ct);
            }

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
                idea.SubmittedAt = DateTime.UtcNow;
                idea.IsDraft = false;
            }
            else if (!isUpdate)
            {
                idea.ReferenceNumber = string.Empty;
                var draftStatus = await _repo.GetStatusByCodeAsync(IdeaStatusCodes.New, ct);
                idea.CurrentStatusId = draftStatus?.Id ?? Guid.Empty;
                idea.SubmittedAt = null;
                idea.IsDraft = true;
            }

            try
            {
                await _repo.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist idea for user {UserId}", userId);
                return IdeaCreateOutcome.Failed("تعذّر حفظ الطلب. حاول مرة أخرى.");
            }

            if (!isUpdate && draftId is { } did && did != Guid.Empty)
            {
                var moved = _attachments.MoveDraftToIdea(userId, did, idea.Id, ct);
                if (moved > 0)
                {
                    try { await _repo.SaveChangesAsync(ct); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Failed to persist moved draft attachments."); }
                }
            }

            if (attachments is { Count: > 0 })
            {
                var attachError = await SaveAttachmentsAsync(idea.Id, userId, attachments, ct);
                if (attachError is not null)
                {
                    return IdeaCreateOutcome.Failed(attachError);
                }
            }

            if (!isSaveDraft && !string.IsNullOrEmpty(idea.ReferenceNumber))
            {
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

        public async Task<IdeaDetailsForEditDto?> GetDraftForEditAsync(Guid ideaId, Guid applicantId, IReadOnlyList<Guid> technologyIds, CancellationToken ct)
        {
            var idea = await _repo.GetDraftByIdAsync(ideaId, applicantId, ct);
            if (idea is null) return null;

            var techs = await _repo.GetDraftTechnologyIdsAsync(ideaId, ct);
            return new IdeaDetailsForEditDto(
                idea.Id,
                idea.Title,
                idea.Description,
                idea.ProblemStatement,
                idea.ProposedSolution,
                idea.ExpectedBenefits,
                idea.RequiredResources,
                idea.InnovationDomainId,
                idea.ExpectedImpactId,
                idea.ExpectedImpactOther,
                idea.TargetAudienceId,
                idea.TargetAudienceOther,
                idea.UsesEmergingTech,
                techs,
                idea.TechnologyOther);
        }

        public async Task<IdeaDetailsDto?> GetDetailsAsync(string referenceNumber, Guid userId, CancellationToken ct)
            => await _repo.GetDetailsAsync(referenceNumber, userId, ct);

        public async Task<IReadOnlyList<IdeaSummaryDto>> GetLatestAsync(int take, CancellationToken ct)
            => await _repo.GetLatestAsync(take, ct);

        public async Task<IdeaLookupsDto> GetLookupsAsync(CancellationToken ct)
            => await _repo.GetLookupsAsync(ct);

        public async Task<UserSummaryDto?> GetUserSummaryAsync(Guid userId, CancellationToken ct)
            => await _repo.GetUserSummaryAsync(userId, ct);

        private static InnovationIdea BuildIdea(
            CreateIdeaRequestDto request,
            Guid userId,
            Guid? departmentId,
            bool isSaveDraft)
        {
            return new InnovationIdea
            {
                Id = Guid.NewGuid(),
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                ProblemStatement = NullIfBlank(request.ProblemStatement),
                ProposedSolution = NullIfBlank(request.ProposedSolution),
                ExpectedBenefits = NullIfBlank(request.ExpectedBenefits),
                ExpectedImpactOther = NullIfBlank(request.ExpectedImpactOther),
                TargetAudienceOther = NullIfBlank(request.TargetAudienceOther),
                UsesEmergingTech = request.UsesEmergingTech,
                TechnologyOther = NullIfBlank(request.TechnologyOther),
                RequiredResources = NullIfBlank(request.RequiredResources),
                InnovationDomainId = request.InnovationDomainId ?? Guid.Empty,
                ExpectedImpactId = request.ExpectedImpactId,
                TargetAudienceId = request.TargetAudienceId,
                ApplicantUserId = userId,
                ApplicantDepartmentId = departmentId,
                IsDraft = isSaveDraft,
                CreatedAt = DateTime.UtcNow,
                SubmittedAt = isSaveDraft ? null : DateTime.UtcNow
            };
        }

        private static void ApplyToIdea(
            InnovationIdea idea,
            CreateIdeaRequestDto request,
            Guid? departmentId,
            bool isSaveDraft)
        {
            idea.Title = request.Title.Trim();
            idea.Description = request.Description.Trim();
            idea.ProblemStatement = NullIfBlank(request.ProblemStatement);
            idea.ProposedSolution = NullIfBlank(request.ProposedSolution);
            idea.ExpectedBenefits = NullIfBlank(request.ExpectedBenefits);
            idea.ExpectedImpactOther = NullIfBlank(request.ExpectedImpactOther);
            idea.TargetAudienceOther = NullIfBlank(request.TargetAudienceOther);
            idea.UsesEmergingTech = request.UsesEmergingTech;
            idea.TechnologyOther = NullIfBlank(request.TechnologyOther);
            idea.RequiredResources = NullIfBlank(request.RequiredResources);
            idea.InnovationDomainId = request.InnovationDomainId ?? Guid.Empty;
            idea.ExpectedImpactId = request.ExpectedImpactId;
            idea.TargetAudienceId = request.TargetAudienceId;
            idea.ApplicantDepartmentId = departmentId;
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