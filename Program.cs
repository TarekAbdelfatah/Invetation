using System.Globalization;
using System.Threading.RateLimiting;
using Ibtikar.Data;
using Ibtikar.Services;
using Ibtikar.Services.Security;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
            });
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddDbContext<IbtikarDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddScoped<Pbkdf2PasswordHasher>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<Ibtikar.Services.AuditLogService>();

            builder.Services.AddIbtikarCookieAuth(builder.Configuration);

            builder.Services.AddRateLimiter(options =>
            {
                options.AddPolicy("login", httpContext =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 6,
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            AutoReplenishment = true
                        }));
            });

            var app = builder.Build();

            ApplyPendingMigrations(app);

            app.UseMiddleware<Ibtikar.Middleware.ExceptionMiddleware>();
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseRequestLocalization(BuildArabicRequestLocalizationOptions());

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }

        private static void ApplyPendingMigrations(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IbtikarDbContext>();
            try
            {
                db.Database.Migrate();
                Ibtikar.Data.Seed.IdeaStatusSeed.SeedIdeaStatuses(db);
                Ibtikar.Data.Seed.InnovationDomainSeed.SeedInnovationDomains(db);
                Ibtikar.Data.Seed.UserTypeSeed.SeedUserTypes(db);
                Ibtikar.Data.Seed.DepartmentSeed.SeedDepartments(db);
                Ibtikar.Data.Seed.RoleSeed.SeedRoles(db);
                Ibtikar.Data.Seed.AssessmentCriterionSeed.SeedCriteria(db);
                Ibtikar.Data.Seed.CriterionScoringSeed.SeedScoring(db);
                Ibtikar.Data.Seed.FormLookupSeed.SeedFormLookups(db);
                db.SaveChanges();
                Ibtikar.Data.Seed.InnovationIdeaSeed.SeedSampleIdeas(db);
                db.SaveChanges();
                Ibtikar.Data.Seed.UserSeed.SeedTestUsers(db, new Pbkdf2PasswordHasher());
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Migration");
                logger.LogWarning(ex, "Database migration/seed skipped: {Message}", ex.Message);
            }
        }

        private static RequestLocalizationOptions BuildArabicRequestLocalizationOptions()
        {
            var arabicCulture = new CultureInfo("ar-SA");
            return new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(arabicCulture),
                SupportedCultures = new List<CultureInfo> { arabicCulture },
                SupportedUICultures = new List<CultureInfo> { arabicCulture },
                FallBackToParentCultures = true,
                ApplyCurrentCultureToResponseHeaders = true
            };
        }
    }
}
