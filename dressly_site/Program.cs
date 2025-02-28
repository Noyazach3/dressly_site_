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

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // הוספת LoginSession לשירותים
            builder.Services.AddScoped<IAdminService, AdminService>();

            builder.Services.AddHttpClient("API", client =>
            {
                // הגדרת כתובת בסיס ל־HttpClient
                client.BaseAddress = new Uri("http://localhost:5177/api/");
            });


            // הוספת שירותי Controllers
            builder.Services.AddControllers();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            // הוספת Middleware עבור Authorization
            app.UseRouting();
            app.UseAuthentication(); // אם יש לך Authentication
            app.UseAuthorization(); // קריטי להפעיל את המדיניות

            app.UseAntiforgery();

            // רישום נתיבים עבור API
            app.MapControllers();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
