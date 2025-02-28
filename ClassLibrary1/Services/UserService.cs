using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using ClassLibrary1.Models;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace ClassLibrary1.Services
{
    public class UserService : IUserService
    {
        private readonly IConfiguration _configuration;

        public UserService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();
            string query = "SELECT * FROM Users WHERE Id = @Id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new User
                {
                    UserID = reader.GetInt32("Id"),
                    Username = reader.GetString("Username"),
                    Email = reader.GetString("Email"),
                    Role = reader.GetString("Role")
                };
            }
            return null;
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();
            string query = "SELECT * FROM Users WHERE Username = @Username";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new User
                {
                    UserID = reader.GetInt32("Id"),
                    Username = reader.GetString("Username"),
                    Email = reader.GetString("Email"),
                    Role = reader.GetString("Role")
                };
            }
            return null;
        }

        public async Task<bool> CreateUserAsync(User user)
        {
            using var connection = new MySqlConnection(GetConnectionString());
            await connection.OpenAsync();
            string query = "INSERT INTO Users (Username, Email, PasswordHash, Role) VALUES (@Username, @Email, @PasswordHash, @Role)";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", user.Username);
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            command.Parameters.AddWithValue("@Role", user.Role);

            int affectedRows = await command.ExecuteNonQueryAsync();
            return affectedRows > 0;
        }
    }
}
