using Ibtikar.DTOs.MyRequests;
using Ibtikar.Repositories;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Interfaces;

namespace Ibtikar.Services.Implementations
{
    public sealed class MyRequestsService : IMyRequestsService
    {
        private const int ListTake = 50;

        private readonly IMyRequestsRepository _repo;
        private readonly FileStorageService _storage;

        public MyRequestsService(IMyRequestsRepository repo, FileStorageService storage)
        {
            _repo = repo;
            _storage = storage;
        }

        public Task<MyRequestsListDto> GetListAsync(Guid applicantId, CancellationToken ct)
            => _repo.GetListAsync(applicantId, ListTake, ct);

        public Task<MyRequestDetailsDto?> GetDetailsAsync(Guid applicantId, Guid id, CancellationToken ct)
            => _repo.GetDetailsAsync(applicantId, id, ct);

        public async Task<MyRequestDeleteResult> DeleteAsync(Guid applicantId, Guid id, CancellationToken ct)
        {
            var idea = await _repo.GetForApplicantAsync(applicantId, id, ct);
            if (idea is null) return new(MyRequestDeleteStatus.NotFound, null);

            if (!IsDeletableNewIdea(idea))
                return new(MyRequestDeleteStatus.NotDeletable, "لا يمكن حذف الطلب بعد أن يبدأ الفريق المختص دراسته.");

            foreach (var attachment in idea.Attachments)
            {
                _storage.Delete(attachment.StoragePath);
            }

            await _repo.RemoveIdeaAsync(idea, ct);
            await _repo.SaveChangesAsync(ct);
            return new(MyRequestDeleteStatus.Success, null);
        }

        public Task<MyRequestResubmitResult> ResubmitCompletionAsync(
            Guid applicantId,
            Guid id,
            MyRequestContentUpdateDto content,
            CancellationToken ct)
            => ResubmitAsync(applicantId, id, content, IdeaStatusCodes.WaitingForCompletion, IdeaStatusCodes.UnderStudy, ct);

        public Task<MyRequestResubmitResult> ResubmitDevelopedAsync(
            Guid applicantId,
            Guid id,
            MyRequestContentUpdateDto content,
            CancellationToken ct)
            => ResubmitAsync(applicantId, id, content, IdeaStatusCodes.ReturnedForDevelopment, IdeaStatusCodes.UnderStudy, ct);

        private async Task<MyRequestResubmitResult> ResubmitAsync(
            Guid applicantId,
            Guid id,
            MyRequestContentUpdateDto content,
            string requiredStatusCode,
            string nextStatusCode,
            CancellationToken ct)
        {
            var idea = await _repo.GetForApplicantAsync(applicantId, id, ct);
            if (idea is null) return new(MyRequestResubmitStatus.NotFound, null);

            if (!string.Equals(idea.CurrentStatus?.Code, requiredStatusCode, StringComparison.OrdinalIgnoreCase))
                return new(MyRequestResubmitStatus.WrongStatus, "لا يمكن إعادة التقديم خارج الحالة المسموح بها.");

            var description = content.Description?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(description))
                return new(MyRequestResubmitStatus.EmptyDescription, "وصف الفكرة مطلوب.");

            var problem = content.ProblemStatement?.Trim();
            var solution = content.ProposedSolution?.Trim();
            var benefits = content.ExpectedBenefits?.Trim();

            if (!IsMaterialChange(idea, description, problem, solution, benefits))
                return new(MyRequestResubmitStatus.NoMaterialChange, "يجب إجراء تغيير حقيقي على فكرة واحدة على الأقل قبل إعادة التقديم.");

            idea.Description = description;
            idea.ProblemStatement = string.IsNullOrWhiteSpace(problem) ? null : problem;
            idea.ProposedSolution = string.IsNullOrWhiteSpace(solution) ? null : solution;
            idea.ExpectedBenefits = string.IsNullOrWhiteSpace(benefits) ? null : benefits;

            var nextId = await _repo.GetStatusIdByCodeAsync(nextStatusCode, ct);
            if (nextId is not null) idea.CurrentStatusId = nextId.Value;

            await _repo.SaveChangesAsync(ct);
            return new(MyRequestResubmitStatus.Success, null);
        }

        private static bool IsMaterialChange(
            Models.InnovationIdea idea,
            string newDescription,
            string? newProblem,
            string? newSolution,
            string? newBenefits)
        {
            if (!string.Equals(idea.Description?.Trim(), newDescription, StringComparison.Ordinal)) return true;
            if (!string.Equals(idea.ProblemStatement?.Trim() ?? string.Empty, newProblem ?? string.Empty, StringComparison.Ordinal)) return true;
            if (!string.Equals(idea.ProposedSolution?.Trim() ?? string.Empty, newSolution ?? string.Empty, StringComparison.Ordinal)) return true;
            if (!string.Equals(idea.ExpectedBenefits?.Trim() ?? string.Empty, newBenefits ?? string.Empty, StringComparison.Ordinal)) return true;
            return false;
        }

        private static bool IsDeletableNewIdea(Models.InnovationIdea idea) =>
            !idea.IsDraft && string.Equals(idea.CurrentStatus?.Code, IdeaStatusCodes.New, StringComparison.OrdinalIgnoreCase);
    }
}