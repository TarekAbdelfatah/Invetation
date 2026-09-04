using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Ibtikar.Services.Helpers
{
    public static class AuthCookieClearer
    {
        private static readonly string[] _targetCookies = 
        {
            ".Ibtikar.Auth", ".AspNetCore.Cookies", ".AspNetCore.Session", 
            "id_token", ".Ibtikar.OidcState", "pkce_verifier", 
            "idsvr.session", "ARRAffinity", "ARRAffinitySameSite"
        };

        public static async Task ClearAsync(HttpContext context, ILogger logger)
        {
            await TrySignOutAsync(context, logger);
            TryClearSession(context, logger);

            var pathsToDelete = GetPathsToDelete(context);
            var cookiesToDelete = context.Request.Cookies.Keys.Union(_targetCookies).Distinct();

            foreach (var key in cookiesToDelete)
            {
                foreach (var path in pathsToDelete)
                {
                    DeleteCookieVariations(context, key, path);
                }
            }
        }

        private static async Task TrySignOutAsync(HttpContext context, ILogger logger)
        {
            try
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SignOutAsync failed during logout.");
            }
        }

        private static void TryClearSession(HttpContext context, ILogger logger)
        {
            try
            {
                context.Session?.Clear();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Session.Clear() failed during logout.");
            }
        }

        private static IEnumerable<string> GetPathsToDelete(HttpContext context)
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "/", "" };
            
            var pathBase = context.Request.PathBase.Value;
            if (!string.IsNullOrEmpty(pathBase))
            {
                paths.Add(pathBase);
                paths.Add($"{pathBase}/");
                paths.Add(pathBase.TrimEnd('/'));
            }

            return paths;
        }

        private static void DeleteCookieVariations(HttpContext context, string key, string path)
        {
            var variations = new CookieOptions[]
            {
                new() { Path = path },
                new() { Path = path, Secure = true },
                new() { Path = path, HttpOnly = true },
                new() { Path = path, Secure = true, HttpOnly = true },
                new() { Path = path, SameSite = SameSiteMode.Lax },
                new() { Path = path, SameSite = SameSiteMode.Lax, Secure = true },
                new() { Path = path, SameSite = SameSiteMode.None, Secure = true }
            };

            foreach (var options in variations)
            {
                try
                {
                    context.Response.Cookies.Delete(key, options);
                }
                catch
                {
                }
            }
        }
    }
}
