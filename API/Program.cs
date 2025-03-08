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
            builder.Services.AddScoped<LoginSession, LoginSession>();
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // הוספת IConfiguration
            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

            // 🔹 **תיקון: חיבור למסד הנתונים MySQL כ-Scoped במקום Singleton**
            builder.Services.AddScoped<MySqlConnection>(_ =>
                new MySqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

            // הוספת שירותי Controllers
            builder.Services.AddControllers();

            // Middleware
            var app = builder.Build();
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
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
