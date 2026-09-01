using System.Globalization;
using System.Threading.RateLimiting;
using Ibtikar.Data;
using Ibtikar.Data.Seed;
using Ibtikar.Middleware;
using Ibtikar.Repositories;
using Ibtikar.Services;
using Ibtikar.Services.Implementations;
using Ibtikar.Services.Hosted;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Notifications;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
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
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            });
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddDbContext<IbtikarDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddScoped<Pbkdf2PasswordHasher>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<AuditLogService>();
            builder.Services.AddSingleton<PdfValidator>();
            builder.Services.AddScoped<FileStorageService>();
            builder.Services.AddScoped<AttachmentService>();
            builder.Services.AddScoped<IPartnerAssignmentQuery, PartnerAssignmentQuery>();
            builder.Services.AddScoped<IIdeaRepository, IdeaRepository>();
            builder.Services.AddScoped<IIdeaService, IdeaService>();
            builder.Services.AddScoped<IAdminOverviewRepository, AdminOverviewRepository>();
            builder.Services.AddScoped<IAdminOverviewService, AdminOverviewService>();
            builder.Services.AddScoped<IMyRequestsRepository, MyRequestsRepository>();
            builder.Services.AddScoped<IMyRequestsService, MyRequestsService>();
            builder.Services.AddScoped<IAuditRepository, AuditRepository>();
            builder.Services.AddScoped<IAuditService, AuditService>();
            builder.Services.AddScoped<ISpecializedDashboardRepository, SpecializedDashboardRepository>();
            builder.Services.AddScoped<Ibtikar.Services.SpecializedDashboard.ISpecializedDashboardService, Ibtikar.Services.SpecializedDashboard.SpecializedDashboardService>();
            builder.Services.AddScoped<IPartnerDashboardRepository, PartnerDashboardRepository>();
            builder.Services.AddScoped<Ibtikar.Services.PartnerDashboard.IPartnerDashboardService, Ibtikar.Services.PartnerDashboard.PartnerDashboardService>();
            builder.Services.AddScoped<Ibtikar.Services.Interfaces.ICommitteeFormationService, Ibtikar.Services.Implementations.CommitteeFormationService>();
            builder.Services.AddScoped<Ibtikar.Repositories.IExecutionRepository, Ibtikar.Repositories.ExecutionRepository>();
            builder.Services.AddScoped<Ibtikar.Services.IExecutionService, Ibtikar.Services.ExecutionService>();
            builder.Services.AddScoped<Ibtikar.Repositories.IReportsRepository, Ibtikar.Repositories.ReportsRepository>();
            builder.Services.AddScoped<Ibtikar.Services.IReportsService, Ibtikar.Services.ReportsService>();
            builder.Services.AddScoped<IDelegationService, DelegationService>();
            builder.Services.AddScoped<ICommitteeDashboardService, CommitteeDashboardService>();
            builder.Services.AddHostedService<VoteLockHostedService>();
            builder.Services.AddHostedService<IdeaDeadlineHostedService>();
            builder.Services.Configure<IntegrationOptions>(builder.Configuration.GetSection("Integrations"));
            builder.Services.AddHttpClient<ProcedureGatewayService>();
            builder.Services.AddHttpClient<INotificationClient, NotificationService>();
            builder.Services.AddOptions<FileStorageOptions>()
                .Configure<IConfiguration>((opts, cfg) =>
                {
                    var v = cfg.GetSection("Integrations").GetValue<string>("AttachmentRoot");
                    if (!string.IsNullOrWhiteSpace(v)) opts.Root = v;
                });

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

            app.UseMiddleware<SecurityHeadersMiddleware>();
            app.UseMiddleware<ExceptionMiddleware>();
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
                AuditSchemaUpgrader.EnsureAuditSchema(db);
                DatabaseSeeder.SeedLookups(db);
                DatabaseSeeder.SeedSampleIdeas(db);
                DatabaseSeeder.SeedTestUsers(db, new Pbkdf2PasswordHasher());
                db.Database.Migrate();
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

    internal static class DatabaseSeeder
    {
        public static void SeedLookups(IbtikarDbContext db)
        {
            IdeaStatusSeed.SeedIdeaStatuses(db);
            InnovationDomainSeed.SeedInnovationDomains(db);
            UserTypeSeed.SeedUserTypes(db);
            DepartmentSeed.SeedDepartments(db);
            RoleSeed.SeedRoles(db);
            AssessmentCriterionSeed.SeedCriteria(db);
            CriterionScoringSeed.SeedScoring(db);
            FormLookupSeed.SeedFormLookups(db);
            db.SaveChanges();
        }

        public static void SeedSampleIdeas(IbtikarDbContext db)
        {
            InnovationIdeaSeed.SeedSampleIdeas(db);
            db.SaveChanges();
        }

        public static void SeedTestUsers(IbtikarDbContext db, Pbkdf2PasswordHasher hasher)
        {
            UserSeed.SeedTestUsers(db, hasher);
        }
    }
}
