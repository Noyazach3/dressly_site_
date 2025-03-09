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
using Microsoft.OpenApi.Models; // 👈 הוספת שימוש ב-Swagger

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

            // 🔹 **תיקון: חיבור למסד הנתונים MySQL כ-Scoped במקום Singleton**
            builder.Services.AddScoped<MySqlConnection>(_ =>
                new MySqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

            // הוספת שירותי Controllers
            builder.Services.AddControllers();

            // 🔹 **תיקון – רישום Swagger בשירותים**
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "API Documentation",
                    Version = "v1"
                });
            });

            // Middleware
            var app = builder.Build();
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Documentation V1");
                    c.RoutePrefix = "swagger"; // הוספת נתיב ישיר לדף ה-UI
                });
            }

            app.UseRouting();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.Run();
        }
    }
}
