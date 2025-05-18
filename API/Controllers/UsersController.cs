using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MySql.Data.MySqlClient;
using System.Data;
using System.Security.Cryptography;
using ClassLibrary1.Services;
using System;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using ClassLibrary1.DTOs;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly LoginSession _loginSession; //מחלקה שמחזיקה את פרטי המשתמש המחובר

        public UsersController(IConfiguration configuration, LoginSession loginSession)
        {
            _config = configuration;
            _connectionString = _config.GetConnectionString("DefaultConnection");
            _loginSession = loginSession;
        }

        [HttpPost("login")] // פעולה להתחברות המשתמש
        public async Task<IActionResult> Login([FromBody] RegisterModel loginRequest)
        {
            // בדיקה אם הקלט תקין
            if (loginRequest == null || string.IsNullOrWhiteSpace(loginRequest.Email) || string.IsNullOrWhiteSpace(loginRequest.PasswordHash))
            {
                return BadRequest("Email or password is missing.");
            }

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // שליפת המשתמש לפי אימייל
            var query = "SELECT UserID, Username, Email, PasswordHash, COALESCE(Role, 'User') AS Role FROM Users WHERE Email = @Email";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Email", loginRequest.Email);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var storedHash = reader.GetString(reader.GetOrdinal("PasswordHash"));

                // בדיקת סיסמה עם VerifyPassword
                if (!VerifyPassword(loginRequest.PasswordHash, storedHash))
                {
                    return Unauthorized("Invalid email or password.");
                }

                // ההתחברות הצליחה - שולפים את הפרטים
                var userID = reader.GetInt32(reader.GetOrdinal("UserID"));
                var username = reader.GetString(reader.GetOrdinal("Username"));
                var email = reader.GetString(reader.GetOrdinal("Email"));
                var role = reader.GetString(reader.GetOrdinal("Role"));

                Console.WriteLine($"🔹 התחברות מוצלחת – Role שהתקבל מה-DB: {role}");

                // שמירת פרטי ההתחברות במחלקת LoginSession
                _loginSession.SetLoginDetails(userID, username, email, role);

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

        // פונקציה שבודקת אם הסיסמה שהוזנה תואמת את ההאש השמור
        private bool VerifyPassword(string enteredPassword, string storedHash)
        {
            var parts = storedHash.Split(':');
            if (parts.Length != 2)
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[0]);
            var storedHashedPassword = parts[1];

            // הצפנה מחדש של הסיסמה שנשלחה
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: enteredPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return hashed == storedHashedPassword;
        }


        // פעולה לרישום משתמש חדש
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel registerModel)
        {
            // בדיקת תקינות בסיסית
            if (registerModel == null || string.IsNullOrWhiteSpace(registerModel.PasswordHash))
            {
                return BadRequest("Invalid registration data.");
            }

            //הצפנת סיסמה עם מלח (salt)
            var passwordHash = HashPassword(registerModel.PasswordHash);

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // בדיקה אם כבר קיים משתמש עם האימייל הזה
            var checkQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
            using var checkCmd = new MySqlCommand(checkQuery, connection);
            checkCmd.Parameters.AddWithValue("@Email", registerModel.Email);
            int userCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
            if (userCount > 0)
            {
                return BadRequest("User already exists");
            }

            // הוספת המשתמש החדש
            var insertQuery = "INSERT INTO Users (Username, Email, PasswordHash, Role) VALUES (@Username, @Email, @PasswordHash, 'User')";
            using var insertCmd = new MySqlCommand(insertQuery, connection);
            insertCmd.Parameters.AddWithValue("@Username", registerModel.Username);
            insertCmd.Parameters.AddWithValue("@Email", registerModel.Email);
            insertCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            await insertCmd.ExecuteNonQueryAsync();

            return Ok("Registration successful.");
        }

        // פונקציה שמבצעת Hash לסיסמה ומחזירה מחרוזת מוצפנת עם Salt
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

        
    }
}
