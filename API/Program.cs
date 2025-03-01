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
            builder.Services.AddScoped<ILoginSession, LoginSession>();
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
                    IsUserRole(context.User.Identity?.Name, "User")));

                options.AddPolicy("AdminOnly", policy => policy.RequireAssertion(context =>
                    IsUserRole(context.User.Identity?.Name, "Admin")));
            });

            // הוספת Authentication עם Cookies והגדרת אפשרויות העוגייה לשיתוף בין פורטים
            builder.Services.AddAuthentication("Cookies")
                .AddCookie("Cookies", options =>
                {
                    options.LoginPath = "/login";
                    options.AccessDeniedPath = "/access-denied";
                    // עדכון הגדרות העוגייה לשיתוף בין אתרים (פורטים)
                    options.Cookie.SameSite = SameSiteMode.None;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // בסביבת Production עדיף Always אם יש HTTPS
                    options.Cookie.Domain = "localhost";
                });

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

            // מיפוי Controllers
            app.MapControllers();

            app.Run();
        }

        // פונקציה לבדיקה אם המשתמש בתפקיד מסוים (User או Admin)
        private static bool IsUserRole(string username, string role)
        {
            if (string.IsNullOrEmpty(username))
                return false;

            // ודא שהחיבור כאן תואם למסד הנתונים שלך
            string connectionString = "Server=localhost;Database=dressly;User=root;Password=Noya0532Zach;";
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
