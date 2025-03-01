using System.Reflection;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using ClassLibrary1.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Antiforgery;

namespace API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // רישום שירותים
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<LoginSession, LoginSession>();
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // הוספת IConfiguration
            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

            // הגדרת CORS - מאפשר קריאות מ־Blazor כולל credentials
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins("http://localhost:57864") // דומיין Blazor
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            // חיבור למסד הנתונים MySQL
            string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddSingleton(new MySqlConnection(connectionString));

            // הוספת שירותי Controllers
            builder.Services.AddControllers();

            // הוספת שירותי Authorization מותאמים אישית
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("UserOnly", policy => policy.RequireAssertion(context =>
                    IsUserRole(context.User.Identity?.Name, "User", builder.Configuration)));

                options.AddPolicy("AdminOnly", policy => policy.RequireAssertion(context =>
                    IsUserRole(context.User.Identity?.Name, "Admin", builder.Configuration)));
            });

            // הוספת Authentication עם Cookies והגדרת אפשרויות העוגייה לשיתוף בין פורטים
            builder.Services.AddAuthentication("Cookies")
                .AddCookie("Cookies", options =>
                {
                    options.LoginPath = "/login";
                    options.AccessDeniedPath = "/access-denied";
                    // הגדרות עוגייה לשיתוף בין אתרים (פורטים שונים)
                    options.Cookie.SameSite = SameSiteMode.None;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // בסביבת Production מומלץ Always עם HTTPS
                    options.Cookie.Domain = "localhost";
                });

            // הוספת שירותי Antiforgery
            builder.Services.AddAntiforgery();

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
            });

            var app = builder.Build();

            // שימוש ב-Swagger בסביבת פיתוח
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Static Files
            app.UseStaticFiles();

            // Middleware
            app.UseRouting();
            app.UseCors();
            app.UseAuthentication(); // קריטי: Authentication לפני Authorization
            app.UseAuthorization();

            // Middleware לאימות Anti-Forgery – יש לשים אותו לאחר Authentication/Authorization ובטרם מיפוי הקונטרולרים
            app.Use(async (context, next) =>
            {
                var endpoint = context.GetEndpoint();
                if (endpoint != null && endpoint.Metadata.GetMetadata<ValidateAntiForgeryTokenAttribute>() != null)
                {
                    var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
                    await antiforgery.ValidateRequestAsync(context);
                }
                await next();
            });

            // מיפוי Controllers
            app.MapControllers();

            app.Run();
        }

        // פונקציה לבדיקה אם המשתמש בתפקיד מסוים (User או Admin) – משתמשת במחרוזת החיבור מהקונפיגורציה
        private static bool IsUserRole(string username, string role, IConfiguration configuration)
        {
            if (string.IsNullOrEmpty(username))
                return false;

            string connectionString = configuration.GetConnectionString("DefaultConnection");
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Role FROM Users WHERE Username = @Username";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    var result = command.ExecuteScalar();
                    return result != null && result.ToString().Equals(role, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }
}
