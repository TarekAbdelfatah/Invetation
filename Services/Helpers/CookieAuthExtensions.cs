using Ibtikar.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.Cookie.Name = ".Ibtikar.Auth";
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.Cookie.HttpOnly = true;
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromHours(2);
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
