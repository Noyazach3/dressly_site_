using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Services;
using ClassLibrary1.Services;
using ClassLibrary1.Models;
using MySql.Data.MySqlClient;
using System.Data;
using System;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly LoginSession _loginSession;
        private readonly string _connectionString;

        public AdminController(IAdminService adminService, LoginSession loginSession, IConfiguration configuration)
        {
            _adminService = adminService;
            _loginSession = loginSession;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private string GetUserRole()
        {
            Console.WriteLine($"🔍 GetUserRole() מחזיר: {_loginSession.Role}");
            return _loginSession.Role;
        }

        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = new List<UserInfoModel>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT * FROM Users";
            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(new UserInfoModel
                {
                    UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                    Username = reader.GetString(reader.GetOrdinal("Username")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    Role = reader.GetString(reader.GetOrdinal("Role"))
                });
            }

            return Ok(users);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                // מחיקת נתוני המשתמש מטבלאות קשורות
                var deleteFromOutfits = "DELETE FROM outfits WHERE UserID = @UserID";
                using var outfitsCommand = new MySqlCommand(deleteFromOutfits, connection, transaction);
                outfitsCommand.Parameters.AddWithValue("@UserID", id);
                await outfitsCommand.ExecuteNonQueryAsync();

                var deleteFromClothingItems = "DELETE FROM clothingitems WHERE UserID = @UserID";
                using var clothingItemsCommand = new MySqlCommand(deleteFromClothingItems, connection, transaction);
                clothingItemsCommand.Parameters.AddWithValue("@UserID", id);
                await clothingItemsCommand.ExecuteNonQueryAsync();

                var deleteFromFavorites = "DELETE FROM favorites WHERE UserID = @UserID";
                using var favoritesCommand = new MySqlCommand(deleteFromFavorites, connection, transaction);
                favoritesCommand.Parameters.AddWithValue("@UserID", id);
                await favoritesCommand.ExecuteNonQueryAsync();

                var deleteFromEvents = "DELETE FROM events WHERE UserID = @UserID";
                using var eventsCommand = new MySqlCommand(deleteFromEvents, connection, transaction);
                eventsCommand.Parameters.AddWithValue("@UserID", id);
                await eventsCommand.ExecuteNonQueryAsync();

                // מחיקת המשתמש עצמו
                var deleteUserQuery = "DELETE FROM Users WHERE UserID = @UserID";
                using var deleteUserCommand = new MySqlCommand(deleteUserQuery, connection, transaction);
                deleteUserCommand.Parameters.AddWithValue("@UserID", id);
                var rowsAffected = await deleteUserCommand.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    await transaction.CommitAsync();
                    return NoContent();
                }
                return NotFound("המשתמש לא נמצא.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"❌ שגיאה במהלך מחיקת המשתמש: {ex.Message}");
                return StatusCode(500, "שגיאה במהלך מחיקת המשתמש.");
            }
        }

        [HttpGet("non-admin-count")]
        public async Task<IActionResult> GetNonAdminUsersCount()
        {
            int nonAdminCount = 0;
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM Users WHERE Role != @Role";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Role", "Admin");

            nonAdminCount = Convert.ToInt32(await command.ExecuteScalarAsync());

            return Ok(new { NonAdminUsersCount = nonAdminCount });
        }
    }
}
