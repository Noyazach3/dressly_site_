using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MySql.Data.MySqlClient;
using System.Data;
using ClassLibrary1.Models; // שימוש במודל הנכון
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using ClassLibrary1.Models;
using ClassLibrary1.Services;

using Console = System.Console; // תיקון ההתנגשות עם BootstrapBlazor.Components.Console

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
        _loginSession = loginSession; // שימוש באובייקט `LoginSession`
    }

    // ------------------------------------------------------------------
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] RegisterModel loginRequest)
    {
        if (loginRequest == null || string.IsNullOrWhiteSpace(loginRequest.Email) || string.IsNullOrWhiteSpace(loginRequest.PasswordHash))
        {
            return BadRequest("Email or password is missing.");
        }

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = "SELECT * FROM Users WHERE Email = @Email";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Email", loginRequest.Email);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var storedHash = reader.GetString("Password");
            if (!VerifyPassword(loginRequest.PasswordHash, storedHash))
            {
                return Unauthorized("Invalid email or password.");
            }

            var userID = reader.GetInt32("UserID");
            var username = reader.GetString("Username");
            var email = reader.GetString("Email");
            var role = reader.GetString("Role");

            Console.WriteLine($"🔹 התחברות מוצלחת – Role שהתקבל מה-DB: {role}");

            // 🔹 **עדכון `LoginSession` עם הנתונים מה-DB**
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

    // ------------------------------------------------------------------
    // פעולה לרישום משתמש חדש (Register)
    [HttpPost("Register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel registerModel)
    {
        if (registerModel == null || string.IsNullOrWhiteSpace(registerModel.PasswordHash))
        {
            return BadRequest("Invalid registration data.");
        }

        var passwordHash = HashPassword(registerModel.PasswordHash); // כמו אצל המורה

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        // בדיקה אם המשתמש כבר קיים
        var checkQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
        using var checkCmd = new MySqlCommand(checkQuery, connection);
        checkCmd.Parameters.AddWithValue("@Email", registerModel.Email);
        int userCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
        if (userCount > 0)
        {
            return BadRequest("User already exists");
        }

        // הוספת המשתמש עם סיסמה מוצפנת
        var insertQuery = "INSERT INTO Users (Username, Email, PasswordHash, Role) VALUES (@Username, @Email, @PasswordHash, @Role)";
        using var insertCmd = new MySqlCommand(insertQuery, connection);
        insertCmd.Parameters.AddWithValue("@Username", registerModel.Username);
        insertCmd.Parameters.AddWithValue("@Email", registerModel.Email);
        insertCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
        insertCmd.Parameters.AddWithValue("@Role", registerModel.Role);
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
}