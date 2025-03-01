using BootstrapBlazor.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MySql.Data.MySqlClient;
using System.Data;
using ClassLibrary1.Models; // שימוש במחלקות מה-Class Library
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;



[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IConfiguration _config;

    public UserController(IConfiguration configuration)
    {
        _config = configuration;
    }



    // ------------------------------------------------------------------
    [HttpGet("ValidateLogin")]
    public IActionResult ValidateLogin([FromQuery] string username, [FromQuery] string passwordHash)
    {
        string connectionString = _config.GetConnectionString("DefaultConnection");

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT PasswordHash FROM users WHERE Username = @Username";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    var storedHash = cmd.ExecuteScalar()?.ToString();

                    if (storedHash == null)
                    {
                        return Unauthorized("Invalid username or password.");
                    }

                    // בדיקת התאמה בין הסיסמה שסופקה להאש השמור במסד הנתונים
                    bool isPasswordValid = VerifyPassword(passwordHash, storedHash);

                    return isPasswordValid ? Ok(true) : Unauthorized("Invalid username or password.");
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred", Error = ex.Message });
        }
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

        var PasswordHash = HashPassword(registerModel.PasswordHash); // כמו אצל המורה

        string connectionString = _config.GetConnectionString("DefaultConnection");

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                await conn.OpenAsync();

                // בדיקה אם המשתמש כבר קיים
                string checkQuery = "SELECT COUNT(*) FROM users WHERE Email = @Email";
                using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@Email", registerModel.Email);
                    int userCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                    if (userCount > 0)
                    {
                        return BadRequest("User already exists");
                    }
                }

                // הוספת המשתמש עם סיסמה מוצפנת
                string insertQuery = "INSERT INTO users (Username, Email, PasswordHash) VALUES (@Username, @Email, @PasswordHash)";
                using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn))
                {
                    insertCmd.Parameters.AddWithValue("@Username", registerModel.Username);
                    insertCmd.Parameters.AddWithValue("@Email", registerModel.Email);
                    insertCmd.Parameters.AddWithValue("@PasswordHash", PasswordHash);
                    await insertCmd.ExecuteNonQueryAsync();
                }

                return Ok("Registration successful.");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred", Error = ex.Message });
        }
    }


    // ------------------------------------------------------------------
    // פעולה לקבלת תפקיד המשתמש
    [HttpGet("GetUserRole")]
    public IActionResult GetUserRole([FromQuery] string username)
    {
        string connectionString = _config.GetConnectionString("DefaultConnection");

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT Role FROM users WHERE Username = @Username";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    var role = cmd.ExecuteScalar();
                    return role != null ? Ok(role.ToString()) : NotFound("User not found");
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred", Error = ex.Message });
        }
    }

    private string HashPassword(string password)
    {
        // Create a random byte array for the salt.
        byte[] salt = new byte[128 / 8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }
        // Hash the password using PBKDF2 with the generated salt.
        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8));
        // Combine the salt and hash for storage.
        return $"{Convert.ToBase64String(salt)}:{hashed}";
    }

    // ------------------------------------------------------------------
    // פעולה למחיקת משתמש (Admin בלבד)
    [HttpDelete("DeleteUser")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult DeleteUser([FromQuery] string username)
    {
        string connectionString = _config.GetConnectionString("DefaultConnection");

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM users WHERE Username = @Username";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0 ? Ok("User deleted successfully") : NotFound("User not found");
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred", Error = ex.Message });
        }
    }
}
