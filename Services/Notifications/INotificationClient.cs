namespace Ibtikar.Services.Notifications
{
    public interface INotificationClient
    {
        Task<bool> SendAsync(string action, string entityId, IDictionary<string, string>? payload = null, CancellationToken ct = default);
    }
}
