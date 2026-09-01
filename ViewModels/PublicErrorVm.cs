namespace Ibtikar.ViewModels
{
    public sealed class PublicErrorVm
    {
        public int Code { get; init; }
        public string Title { get; init; } = "حدث خطأ";
        public string Message { get; init; } = "حدث خطأ غير متوقع. تم تسجيل الحادثة وسيتم معالجتها قريباً.";
        public string Icon { get; init; } = "error_outline";
        public string HomeHref { get; init; } = "/";
        public string? RequestId { get; init; }
        public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);
        public bool ShowException { get; init; }
        public string? ExceptionMessage { get; init; }
    }
}
