using dressly_site2.Components;
using API.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace dressly_site
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // רישום שירותים:
            builder.Services.AddScoped<ClothingService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ClassLibrary1.Services.ILoginSession, ClassLibrary1.Services.LoginSession>();

            // הוספת שירותי Razor Components (Blazor Server)
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // הוספת שירותי מנהל
            builder.Services.AddScoped<IAdminService, AdminService>();

            // הגדרת HttpClient עבור קריאות ל־API
            builder.Services.AddHttpClient("API", client =>
            {
                client.BaseAddress = new Uri("http://localhost:5177/api/");
            });

            // הוספת Controllers עם Views – הסרת הפילטר האוטומטי לאנטי‑פורג'רי
            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Remove(new AutoValidateAntiforgeryTokenAttribute());
            });

            // הגדרת Authentication עם Cookies
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    options.AccessDeniedPath = "/access-denied";
                });

            // הגדרת CORS – אם נדרש (במקרה של עבודה תחת אותו אוריג'ין אין צורך אמיתי)
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins("http://localhost:57864")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            // Middleware:
            app.UseRouting();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();

            // אין כאן Middleware לאנטי‑פורג'רי – כך שאין Endpoint שמחייב אותו

            // מיפוי הנתיבים:
            app.MapControllers();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
