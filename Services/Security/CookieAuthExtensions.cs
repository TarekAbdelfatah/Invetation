using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

namespace Ibtikar.Services.Security
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

            services
                .AddAuthentication(Scheme)
                .AddCookie(Scheme, options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
                    options.SlidingExpiration = false;
                    options.Cookie.Name = "ibtikar.auth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.ReturnUrlParameter = "returnUrl";
                });

            services.AddAuthorization();
            return services;
        }
    }
}
