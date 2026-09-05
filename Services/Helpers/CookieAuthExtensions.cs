using Ibtikar.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;

namespace Ibtikar.Services.Helpers
{
    public static class CookieAuthExtensions
    {
        public const string Scheme = CookieAuthenticationDefaults.AuthenticationScheme;

        public static IServiceCollection AddIbtikarCookieAuth(this IServiceCollection services, IConfiguration configuration)
        {
            var keysPath = configuration["DataProtection:KeysPath"] ?? "Keys";
            Directory.CreateDirectory(keysPath);

            services
                .AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
                .SetApplicationName("Ibtikar");

            services.Configure<SsoSettingsOptions>(
                configuration.GetSection("IdentityServer"));

            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = "Cookies";
                    options.DefaultChallengeScheme = "oidc";
                })
                .AddCookie("Cookies", options =>
                {
                    options.Cookie.Name = ".Ibtikar.Auth";
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.Cookie.HttpOnly = true;
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/Login";
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.SlidingExpiration = false;
                })
                .AddOpenIdConnect("oidc", options =>
                {
                    var ssoConfig = configuration.GetSection("IdentityServer");
                    options.Authority = ssoConfig["Authority"];
                    options.ClientId = ssoConfig["ClientId"];

                    options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;

                    options.ResponseType = "code";
                    options.ResponseMode = null!;
                    options.UsePkce = true;
                    options.DisableTelemetry = true; // removes x-client-SKU / x-client-ver

                    options.CallbackPath = "/signin-callback";

                    options.CorrelationCookie.Name = ".Ibtikar.Correlation";
                    options.CorrelationCookie.SameSite = SameSiteMode.None;
                    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;

                    options.NonceCookie.Name = ".Ibtikar.Nonce";
                    options.NonceCookie.SameSite = SameSiteMode.None;
                    options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;

                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.SaveTokens = true;
                    options.RequireHttpsMetadata = true;
                    options.DisableTelemetry = true; // removes x-client-SKU / x-client-ver

                    options.Events = new OpenIdConnectEvents
                    {
                        OnTicketReceived = context =>
                        {
                            context.ReturnUri = "/signin-complete";
                            return Task.CompletedTask;
                        },
                        OnRemoteFailure = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("OIDC");
                            logger.LogError(context.Failure, "OIDC remote authentication failure: {Error}", context.Failure?.Message);
                            // Redirect to home with error parameter to prevent infinite login loop
                            context.Response.Redirect("/?error=" + Uri.EscapeDataString(context.Failure?.Message ?? "auth_failed"));
                            context.HandleResponse();
                            return Task.CompletedTask;
                        }
                    };
                })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    var ssoConfig = configuration.GetSection("IdentityServer");
                    options.Authority = ssoConfig["Authority"];
                    options.RequireHttpsMetadata = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false,
                        ValidateIssuer = true
                    };
                });

            services.AddAuthorization();
            return services;
        }
    }
}
