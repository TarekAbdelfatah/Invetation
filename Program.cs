using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace Ibtikar
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

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
