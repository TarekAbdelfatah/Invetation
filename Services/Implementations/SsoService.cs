using Ibtikar.DTOs;
using Ibtikar.Options;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ibtikar.Services.Implementations
{
    public class SsoService
    {
        private readonly HttpClient _httpClient;
        private readonly SsoSettingsOptions _settings;

        public SsoService(HttpClient httpClient, IOptions<SsoSettingsOptions> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public string GetSettingsAuthority() => EnsureHttps(_settings.Authority);
        public string GetClientId() => _settings.ClientId;
        public string GetRedirectUri() => EnsureHttps(_settings.RedirectUri);

        private static string EnsureHttps(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                return "https://" + url[7..];
            }
            return url;
        }

        /// <summary>
        /// Builds the OIDC authorization URL using IdentityModel's RequestUrl builder.
        /// This ensures spec-compliant, properly encoded query parameters.
        /// </summary>
        public string BuildAuthorizeUrl(
            string redirectUri,
            string state,
            string nonce,
            string codeChallenge,
            string codeChallengeMethod = OidcConstants.CodeChallengeMethods.Sha256,
            string scope = "openid profile")
        {
            var authority = EnsureHttps(_settings.Authority);
            var authorizeEndpoint = $"{authority.TrimEnd('/')}/{_settings.AuthorizeEndpoint.TrimStart('/')}";
            var secureRedirectUri = EnsureHttps(redirectUri);

            var ru = new RequestUrl(authorizeEndpoint);
            return ru.CreateAuthorizeUrl(
                clientId: _settings.ClientId,
                responseType: OidcConstants.ResponseTypes.Code,
                scope: scope,
                redirectUri: secureRedirectUri,
                state: state,
                nonce: nonce,
                codeChallenge: codeChallenge,
                codeChallengeMethod: codeChallengeMethod);
        }

        /// <summary>
        /// Builds the end-session (logout) URL using IdentityModel's RequestUrl builder.
        /// </summary>
        public string BuildLogoutUrl(string postLogoutRedirectUri, string? idTokenHint = null)
        {
            var authority = EnsureHttps(_settings.Authority);
            var endSessionEndpoint = $"{authority.TrimEnd('/')}/{_settings.EndSessionEndpoint.TrimStart('/')}";
            var securePostLogoutUri = EnsureHttps(postLogoutRedirectUri);

            var ru = new RequestUrl(endSessionEndpoint);
            return ru.CreateEndSessionUrl(
                idTokenHint: idTokenHint,
                postLogoutRedirectUri: securePostLogoutUri,
                extra: new Parameters(new[] { KeyValuePair.Create("client_id", _settings.ClientId) }));
        }

        /// <summary>
        /// Retrieves user profile from the OIDC userinfo endpoint safely with case-insensitive JSON deserialization.
        /// </summary>
        public async Task<SSoUserInfo?> GetSSOUserInfoAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token cannot be null or empty", nameof(token));

            var authority = EnsureHttps(_settings.Authority);
            var userInfoEndpoint = $"{authority.TrimEnd('/')}/{_settings.UserInfo.TrimStart('/')}";

            var request = new HttpRequestMessage(HttpMethod.Get, userInfoEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"SSO Response Status: {response.StatusCode}, Details: {content}");
            }

            return JsonSerializer.Deserialize<SSoUserInfo>(content);
        }

        /// <summary>
        /// Exchanges authorization code for tokens and parses token expiration and claims.
        /// </summary>
        public async Task<SsoTokenResult> ExchangeCodeForTokenDetailsAsync(string code, string redirectUri, string? codeVerifier = null)
        {
            var authority = EnsureHttps(_settings.Authority);
            var tokenEndpoint = $"{authority.TrimEnd('/')}/{_settings.TokenEndpoint.TrimStart('/')}";
            var secureRedirectUri = EnsureHttps(redirectUri);

            var response = await _httpClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
            {
                Address = tokenEndpoint,
                ClientId = _settings.ClientId,
                Code = code,
                RedirectUri = secureRedirectUri,
                CodeVerifier = codeVerifier
            });

            if (response.IsError)
                throw new Exception($"Token Exchange Failed: {response.Error} — {response.ErrorDescription}");

            return new SsoTokenResult
            {
                AccessToken = response.AccessToken ?? string.Empty,
                IdentityToken = response.IdentityToken,
                ExpiresIn = response.ExpiresIn,
                ExpiresUtc = response.ExpiresIn > 0 ? DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn) : null
            };
        }
    }
}
