using BootstrapBlazor.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MySql.Data.MySqlClient;
using System.Data;
using ClassLibrary1.Models; // שימוש במחלקות מה-Class Library

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
    // פעולה לאימות משתמש (ValidateLogin)
    [HttpGet("ValidateLogin")]
    public IActionResult ValidateLogin([FromQuery] string username, [FromQuery] string passwordHash)
    {
        string connectionString = _config.GetConnectionString("DefaultConnection");

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM users WHERE Username = @Username AND PasswordHash = @PasswordHash";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    int userCount = Convert.ToInt32(cmd.ExecuteScalar());
                    return Ok(userCount > 0);
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred", Error = ex.Message });
        }
    }

    // ------------------------------------------------------------------
    // פעולה לרישום משתמש חדש (Register)
    [HttpPost("Register")]
    public IActionResult Register([FromBody] User newUser)
    {
        string connectionString = _config.GetConnectionString("DefaultConnection");

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // בדיקה אם המשתמש כבר קיים
                string checkQuery = "SELECT COUNT(*) FROM users WHERE Username = @Username";

                using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@Username", newUser.Username);
                    int userCount = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (userCount > 0)
                    {
                        return BadRequest("User already exists");
                    }
                }

                // הוספת המשתמש החדש עם תפקיד User
                string insertQuery = "INSERT INTO users (Username, PasswordHash, Email, Role) VALUES (@Username, @PasswordHash, @Email, 'User')";
                using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn))
                {
                    insertCmd.Parameters.AddWithValue("@Username", newUser.Username);
                    insertCmd.Parameters.AddWithValue("@PasswordHash", newUser.PasswordHash);
                    insertCmd.Parameters.AddWithValue("@Email", newUser.Email);
                    insertCmd.ExecuteNonQuery();
                }
                return Ok("User registered successfully");
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
