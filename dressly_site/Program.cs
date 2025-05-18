using dressly_site2.Components;
using ClassLibrary1.Services; // שירותים כלליים שמשותפים לכל הפרויקט (לוגיקה עסקית, מחלקות עזר)

namespace dressly_site
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            // הרשמת תמיכה ב-Razor Pages ורכיבי Blazor אינטראקטיביים
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddRazorPages();

            // מאפשר גישה לפרטי ה-HttpContext (למשל לצורך גישה למידע על המשתמש)
            builder.Services.AddHttpContextAccessor();

            // הרשמה של LoginSession בתור Singleton כדי לשמור מידע על המשתמש המחובר (למשל מזהה, תפקיד וכו')
            builder.Services.AddSingleton<LoginSession>();

            // הרשמה של HttpClient מותאם אישית עם כותרת "User-Role" מתוך LoginSession
            // משמש לשליחת בקשות API תוך שמירה על הקשר משתמש
            builder.Services.AddScoped(sp =>
            {
                var loginSession = sp.GetRequiredService<LoginSession>();
                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri("http://localhost:5177/") // הכתובת של ה-API
                };
                httpClient.DefaultRequestHeaders.Add("User-Role", loginSession.Role); // הוספת תפקיד המשתמש לכותרת
                return httpClient;
            });

            // ✅ הוספת IHttpClientFactory – מאפשר להשתמש ב-ClientFactory בעמודים (כמו Signup)
            builder.Services.AddHttpClient();

            // תמיכה ב-Controllers עבור API פנימי בתוך ה-Blazor App
            builder.Services.AddControllers();

            var app = builder.Build();

            // ניהול חריגות ב-production
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            // תמיכה בקבצים סטטיים (CSS, תמונות, JS אם יש)
            app.UseStaticFiles();

            app.UseRouting();

            // הגנות ואימותים (אם יופעלו)
            app.UseAuthentication();
            app.UseAuthorization();

            // הגנה מפני התקפות CSRF
            app.UseAntiforgery();

            // מיפוי ה-API Controllers
            app.MapControllers();

            // הגדרת רכיב השורש של Blazor
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
