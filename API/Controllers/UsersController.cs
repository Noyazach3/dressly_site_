using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MySql.Data.MySqlClient;
using System.Data;
using System.Security.Cryptography;
using ClassLibrary1.Services;
using System;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using ClassLibrary1.DTOs;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly LoginSession _loginSession;

        public UsersController(IConfiguration configuration, LoginSession loginSession)
        {
            _config = configuration;
            _connectionString = _config.GetConnectionString("DefaultConnection");
            _loginSession = loginSession;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] RegisterModel loginRequest)
        {
            if (loginRequest == null || string.IsNullOrWhiteSpace(loginRequest.Email) || string.IsNullOrWhiteSpace(loginRequest.PasswordHash))
            {
                return BadRequest("Email or password is missing.");
            }

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT UserID, Username, Email, PasswordHash, COALESCE(Role, 'User') AS Role FROM Users WHERE Email = @Email";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Email", loginRequest.Email);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var storedHash = reader.GetString(reader.GetOrdinal("PasswordHash"));
                if (!VerifyPassword(loginRequest.PasswordHash, storedHash))
                {
                    return Unauthorized("Invalid email or password.");
                }

                var userID = reader.GetInt32(reader.GetOrdinal("UserID"));
                var username = reader.GetString(reader.GetOrdinal("Username"));
                var email = reader.GetString(reader.GetOrdinal("Email"));
                var role = reader.GetString(reader.GetOrdinal("Role"));

                Console.WriteLine($"🔹 התחברות מוצלחת – Role שהתקבל מה-DB: {role}");
                _loginSession.SetLoginDetails(userID, username, email, role);
                Console.WriteLine($"✅ LoginSession.Role מעודכן ל- {_loginSession.Role}");

                return Ok(new
                {
                    Message = "Login successful",
                    UserID = userID,
                    Username = username,
                    Email = email,
                    Role = role
                });
            }

            return Unauthorized("Invalid email or password.");
        }

        private bool VerifyPassword(string enteredPassword, string storedHash)
        {
            var parts = storedHash.Split(':');
            if (parts.Length != 2)
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[0]);
            var storedHashedPassword = parts[1];

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: enteredPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return hashed == storedHashedPassword;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel registerModel)
        {
            if (registerModel == null || string.IsNullOrWhiteSpace(registerModel.PasswordHash))
            {
                return BadRequest("Invalid registration data.");
            }

            var passwordHash = HashPassword(registerModel.PasswordHash);

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var checkQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
            using var checkCmd = new MySqlCommand(checkQuery, connection);
            checkCmd.Parameters.AddWithValue("@Email", registerModel.Email);
            int userCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
            if (userCount > 0)
            {
                return BadRequest("User already exists");
            }

            var insertQuery = "INSERT INTO Users (Username, Email, PasswordHash, Role) VALUES (@Username, @Email, @PasswordHash, 'User')";
            using var insertCmd = new MySqlCommand(insertQuery, connection);
            insertCmd.Parameters.AddWithValue("@Username", registerModel.Username);
            insertCmd.Parameters.AddWithValue("@Email", registerModel.Email);
            insertCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            await insertCmd.ExecuteNonQueryAsync();

            return Ok("Registration successful.");
        }

        private string HashPassword(string password)
        {
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return $"{Convert.ToBase64String(salt)}:{hashed}";
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetUserPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("שם המשתמש והאימייל נדרשים.");
            }

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                var checkUserQuery = "SELECT UserID FROM Users WHERE Username = @Username AND Email = @Email";
                using var checkUserCommand = new MySqlCommand(checkUserQuery, connection, transaction);
                checkUserCommand.Parameters.AddWithValue("@Username", request.Username);
                checkUserCommand.Parameters.AddWithValue("@Email", request.Email);

                var userId = await checkUserCommand.ExecuteScalarAsync();
                if (userId == null)
                {
                    return NotFound("שם המשתמש או האימייל אינם תואמים.");
                }

                string tempPassword = "1234";
                string hashedPassword = HashPassword(tempPassword);

                var query = "UPDATE Users SET PasswordHash = @NewPasswordHash WHERE Username = @Username AND Email = @Email";
                using var command = new MySqlCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@NewPasswordHash", hashedPassword);
                command.Parameters.AddWithValue("@Username", request.Username);
                command.Parameters.AddWithValue("@Email", request.Email);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                {
                    await transaction.CommitAsync();
                    return Ok($"סיסמת המשתמש אופסה בהצלחה. סיסמה זמנית: {tempPassword}");
                }
                return NotFound("המשתמש לא נמצא.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"❌ שגיאה במהלך איפוס הסיסמה: {ex.Message}");
                return StatusCode(500, "שגיאה במהלך איפוס הסיסמה.");
            }
        }
    }
}
