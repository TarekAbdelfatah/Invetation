namespace Ibtikar.DTOs
{
    public class SsoTokenResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? IdentityToken { get; set; }
        public int ExpiresIn { get; set; }
        public DateTimeOffset? ExpiresUtc { get; set; }
    }
}
