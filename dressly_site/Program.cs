using dressly_site2.Components;
using ClassLibrary1.Services; // לוודא שהממשק ILoginSession והמחלקה LoginSession קיימים ונגישים

namespace dressly_site
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddScoped<ClothingService>();

            // הוספת שירותי Blazor Server ו-Razor Pages
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddRazorPages();

            // הרשמת IHttpContextAccessor הנדרש לשירות LoginSession
            builder.Services.AddHttpContextAccessor();

            // הרשמת שירות LoginSession שמממש את ILoginSession
            builder.Services.AddSingleton<LoginSession>();

            builder.Services.AddScoped(sp =>
            {
                var loginSession = sp.GetRequiredService<LoginSession>();
                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri("http://localhost:5177/")
                };
                // הוספת הכותרת "User-Role" לכל בקשה
                httpClient.DefaultRequestHeaders.Add("User-Role", loginSession.Role);
                return httpClient;
            });

            // הוספת שירותי Controllers
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
