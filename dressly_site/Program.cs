using dressly_site2.Components;
using API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using ClassLibrary1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace dressly_site
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ✅ הוספת שירותי Blazor Server ו-Razor Pages
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddRazorPages();  // 📌 זה פותר את השגיאה

            // ✅ הוספת Authentication עם Cookies
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    options.AccessDeniedPath = "/access-denied";
                });

            // ✅ הוספת HttpClient לשימוש ב-API
            builder.Services.AddScoped(sp =>
            {
                var loginSession = sp.GetRequiredService<LoginSession>();
                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri("http://localhost:5177/api")
                };
                httpClient.DefaultRequestHeaders.Add("User-Role", loginSession.Role);
                return httpClient;
            });

            // ✅ הוספת שירותי Controllers עם ביטול Anti-Forgery
            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add(new IgnoreAntiforgeryTokenAttribute());
            });

            var app = builder.Build();

            // ✅ מצב דיבאג
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            // ✅ הפעלת Static Files
            app.UseStaticFiles();

            // ✅ Middleware - לפי סדר נכון:
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAntiforgery();

            // ✅ מיפוי הנתיבים:
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();  // API Controllers
                endpoints.MapBlazorHub();   // Blazor Server
                endpoints.MapFallbackToFile("index.html"); // 📌 תיקון השגיאה
            });

            app.Run();
        }
    }
}
