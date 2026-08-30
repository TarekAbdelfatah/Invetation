using System.Globalization;
using Ibtikar.Data;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddDbContext<IbtikarDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            var app = builder.Build();

            ApplyPendingMigrations(app);

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseRequestLocalization(BuildArabicRequestLocalizationOptions());

            app.UseHttpsRedirection();
            app.UseRouting();

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
                db.SaveChanges();
                Ibtikar.Data.Seed.InnovationDomainSeed.SeedInnovationDomains(db);
                db.SaveChanges();
                Ibtikar.Data.Seed.InnovationIdeaSeed.SeedSampleIdeas(db);
                db.SaveChanges();
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
