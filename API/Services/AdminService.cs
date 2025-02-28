using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClassLibrary1.Models;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace API.Services
{
    public class AdminService : IAdminService
    {
        private readonly IConfiguration _configuration;

        public AdminService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection");
        }

        // שליפת כל המשתמשים במערכת
        public async Task<List<User>> GetUsersAsync()
        {
            List<User> users = new();
            using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();
            string query = "SELECT Id, Username, Email, Role FROM Users";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserID = reader.GetInt32("Id"),
                    Username = reader.GetString("Username"),
                    Email = reader.GetString("Email"),
                    Role = reader.GetString("Role")
                });
            }
            return users;
        }

        // מחיקת משתמש לפי שם משתמש
        public async Task<bool> DeleteUserAsync(string username)
        {
            using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();
            string query = "DELETE FROM Users WHERE Username = @Username";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);
            int affectedRows = await command.ExecuteNonQueryAsync();
            return affectedRows > 0;
        }

        // איפוס סיסמה של משתמש (ברירת מחדל: "123456")
        public async Task<bool> ResetPasswordAsync(string username)
        {
            string defaultPassword = "123456";
            using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();
            string query = "UPDATE Users SET PasswordHash = @PasswordHash WHERE Username = @Username";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@PasswordHash", defaultPassword);
            command.Parameters.AddWithValue("@Username", username);
            int affectedRows = await command.ExecuteNonQueryAsync();
            return affectedRows > 0;
        }
    }
}
