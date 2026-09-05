using Ibtikar.DTOs;
using Ibtikar.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;

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

        public string GetSettingsAuthority() => _settings.Authority;
        public string GetClientId() => _settings.ClientId;

        /// <summary>
        /// Retrieves user profile from the OIDC userinfo endpoint.
        /// </summary>
        public async Task<SSoUserInfo?> GetSSOUserInfoAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token cannot be null or empty", nameof(token));

            var userInfoEndpoint = $"{_settings.Authority.TrimEnd('/')}/{_settings.UserInfo.TrimStart('/')}";

            var request = new HttpRequestMessage(HttpMethod.Get, userInfoEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new Exception($"SSO Response Status: {response.StatusCode}, Details: {content}");
            }

            return await response.Content.ReadFromJsonAsync<SSoUserInfo>();
        }
    }
}
