namespace Ibtikar.Options
{
    public class SsoSettingsOptions
    {
        public string UserInfo { get; set; } = "connect/userinfo";
        public string Authority { get; set; } = "https://webservices.bog.gov.sa/ssotest";
        public string ClientId { get; set; } = "Ibtkar";
        public string ClientSecret { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = "/signin-callback";
        public string AuthorizeEndpoint { get; set; } = "connect/authorize";
        public string TokenEndpoint { get; set; } = "connect/token";
        public string EndSessionEndpoint { get; set; } = "connect/endsession";
        public string? PostLogoutRedirectUri { get; set; }
    }
}
