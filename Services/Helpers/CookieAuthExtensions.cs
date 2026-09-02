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
        public const string Scheme = "Cookies";

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
                    options.ExpireTimeSpan = TimeSpan.FromHours(2);
                })
                .AddOpenIdConnect("oidc", options =>
                {
                    var ssoConfig = configuration.GetSection("IdentityServer");
                    options.Authority = ssoConfig["Authority"];
                    options.ClientId = ssoConfig["ClientId"];

                    options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;

                    options.ResponseType = "code";
                    options.UsePkce = true;

                    // redirect_uri = {baseUrl}/signin-callback — must be whitelisted in IdentityServer
                    options.CallbackPath = "/signin-callback";

                    options.CorrelationCookie.Name = ".Ibtikar.Correlation";
                    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

                    options.NonceCookie.Name = ".Ibtikar.Nonce";
                    options.NonceCookie.SameSite = SameSiteMode.Lax;
                    options.NonceCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.SaveTokens = true;
                    options.RequireHttpsMetadata = true;

                    options.Events = new OpenIdConnectEvents
                    {
                        OnRedirectToIdentityProvider = context =>
                        {
                            // Strip Microsoft IdentityModel telemetry parameters (x-client-SKU & x-client-ver)
                            context.ProtocolMessage.Parameters.Remove("x-client-SKU");
                            context.ProtocolMessage.Parameters.Remove("x-client-ver");

                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("OIDC");
                            logger.LogInformation("[TraceId:{TraceId}] Redirecting to IdentityServer authorize endpoint. ClientId:{ClientId}, RedirectUri:{RedirectUri}",
                                context.HttpContext.TraceIdentifier, options.ClientId, context.ProtocolMessage.RedirectUri);
                            return Task.CompletedTask;
                        },
                        OnAuthorizationCodeReceived = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("OIDC");
                            logger.LogInformation("[TraceId:{TraceId}] Authorization code received from IdentityServer callback.",
                                context.HttpContext.TraceIdentifier);
                            return Task.CompletedTask;
                        },
                        OnTokenResponseReceived = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("OIDC");
                            if (!string.IsNullOrEmpty(context.TokenEndpointResponse?.Error))
                            {
                                logger.LogError("[TraceId:{TraceId}] Token response error: {Error} - {ErrorDescription}",
                                    context.HttpContext.TraceIdentifier, context.TokenEndpointResponse.Error, context.TokenEndpointResponse.ErrorDescription);
                            }
                            else
                            {
                                logger.LogInformation("[TraceId:{TraceId}] Token response received successfully. HasAccessToken:{HasToken}, HasIdToken:{HasIdToken}",
                                    context.HttpContext.TraceIdentifier,
                                    !string.IsNullOrEmpty(context.TokenEndpointResponse?.AccessToken),
                                    !string.IsNullOrEmpty(context.TokenEndpointResponse?.IdToken));
                            }
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("OIDC");
                            var name = context.Principal?.Identity?.Name;
                            logger.LogInformation("[TraceId:{TraceId}] Token validated successfully for Principal Name:{Name}",
                                context.HttpContext.TraceIdentifier, name);
                            return Task.CompletedTask;
                        },
                        OnTicketReceived = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("OIDC");
                            logger.LogInformation("[TraceId:{TraceId}] Ticket received. Setting ReturnUri to /signin-complete",
                                context.HttpContext.TraceIdentifier);
                            context.ReturnUri = "/signin-complete";
                            return Task.CompletedTask;
                        },
                        OnRemoteFailure = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("OIDC");
                            logger.LogError(context.Failure, "[TraceId:{TraceId}] OIDC remote authentication failure: {Error}",
                                context.HttpContext.TraceIdentifier, context.Failure?.Message);
                            context.Response.Redirect("/Account/Login?error=" + Uri.EscapeDataString(context.Failure?.Message ?? "sso_failed"));
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
