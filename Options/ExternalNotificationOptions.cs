namespace Ibtikar.Options
{
    public class ExternalNotificationOptions
    {
        public bool SmsEnabled { get; set; } = true;
        public string SmsEndpoint { get; set; } = string.Empty;
        public bool EmailEnabled { get; set; } = true;
        public string EmailEndpoint { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 10;
        public string AppKey { get; set; } = string.Empty;
        public string GatewayApiKey { get; set; } = string.Empty;
    }
}
