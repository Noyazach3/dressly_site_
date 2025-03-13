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
using Microsoft.OpenApi.Models;

namespace API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ✅ 1. רישום חיבור למסד הנתונים - טעינה רק כאשר צריך
            builder.Services.AddScoped<MySqlConnection>(_ =>
                new MySqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

            // ✅ 2. הוספת שירותים
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<LoginSession, LoginSession>();
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // ✅ 3. הוספת IConfiguration
            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

            // ✅ 4. הוספת Controllers עם טיפול בבעיות סדר ב-JSON
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
                    options.JsonSerializerOptions.PropertyNamingPolicy = null; // שמירת שמות מקוריים
                });

            // ✅ 5. הוספת Swagger עם פתרון לקונפליקטים של שמות מודלים
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "API Documentation",
                    Version = "v1"
                });

                // פתרון למודלים עם שמות כפולים
                options.CustomSchemaIds(type => type.FullName);
            });

            var app = builder.Build();

            // ✅ 6. Middleware של Swagger
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Documentation V1");
                    c.RoutePrefix = "swagger"; // מאפשר כניסה ישירה לדף Swagger
                });
            }

            // ✅ 7. Middleware של אבטחה והרשאות
            app.UseRouting();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();

            // ✅ 8. מיפוי הבקרים
            app.MapControllers();

            app.Run();
        }
    }
}
