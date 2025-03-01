using dressly_site2.Components;
using API.Services;

namespace dressly_site
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddScoped<ClothingService>();

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ClassLibrary1.Services.ILoginSession, ClassLibrary1.Services.LoginSession>();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // הוספת LoginSession ושירותי מנהל
            builder.Services.AddScoped<IAdminService, AdminService>();

            builder.Services.AddHttpClient("API", client =>
            {
                // הגדרת כתובת בסיס ל־HttpClient (שמופעלת עבור ה־API)
                client.BaseAddress = new Uri("http://localhost:5177/api/");
            });

            // הוספת Controllers אם נדרש
            builder.Services.AddControllers();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseAntiforgery();

            app.MapControllers();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
