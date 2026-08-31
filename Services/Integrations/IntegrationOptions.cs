namespace Ibtikar.Services.Integrations
{
    public class IntegrationOptions
    {
        public string ProceduresEndpoint { get; set; } = string.Empty;
        public string ProceduresApiKey { get; set; } = string.Empty;
        public int ProceduresTimeoutSeconds { get; set; } = 5;
        public int ProceduresRetryCount { get; set; } = 1;
        public string NotificationEndpoint { get; set; } = string.Empty;
        public string NotificationApiKey { get; set; } = string.Empty;
        public int NotificationTimeoutSeconds { get; set; } = 5;
        public string AttachmentRoot { get; set; } = string.Empty;
        public int AttachmentMaxBytes { get; set; } = 5 * 1024 * 1024;
        public int AttachmentMaxCount { get; set; } = 2;
    }
}
