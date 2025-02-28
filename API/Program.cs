using System.Reflection;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using ClassLibrary1.Services;

namespace API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ILoginSession, LoginSession>();
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // הוספת IConfiguration
            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

            // הגדרת CORS - מאפשר קריאות מ־Blazor
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins("http://localhost:57864") // דומיין Blazor
                          .AllowAnyHeader()
                          .AllowAnyMethod();
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

            string connectionString = "Server=localhost;Database=your_database;User=root;Password=your_password;";
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Role FROM Users WHERE Username = @Username";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    var result = command.ExecuteScalar();
                    return result != null && result.ToString() == role;
                }
            }
        }
    }
}
