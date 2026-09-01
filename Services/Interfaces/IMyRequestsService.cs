using Ibtikar.DTOs.MyRequests;

namespace Ibtikar.Services.Interfaces
{
    public interface IMyRequestsService
    {
        Task<MyRequestsListDto> GetListAsync(Guid applicantId, int page, int pageSize, CancellationToken ct);
        Task<MyRequestDetailsDto?> GetDetailsAsync(Guid applicantId, Guid id, CancellationToken ct);

        Task<MyRequestDeleteResult> DeleteAsync(Guid applicantId, Guid id, CancellationToken ct);

        Task<MyRequestResubmitResult> ResubmitCompletionAsync(
            Guid applicantId,
            Guid id,
            MyRequestContentUpdateDto content,
            CancellationToken ct);

        Task<MyRequestResubmitResult> ResubmitDevelopedAsync(
            Guid applicantId,
            Guid id,
            MyRequestContentUpdateDto content,
            CancellationToken ct);
    }

    public enum MyRequestDeleteStatus
    {
        Success,
        NotFound,
        NotDeletable
    }

    public sealed record MyRequestDeleteResult(MyRequestDeleteStatus Status, string? Message);

    public enum MyRequestResubmitStatus
    {
        Success,
        NotFound,
        WrongStatus,
        EmptyDescription,
        NoMaterialChange
    }

    public sealed record MyRequestResubmitResult(MyRequestResubmitStatus Status, string? Message);
}