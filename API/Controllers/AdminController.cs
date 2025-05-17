using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ClassLibrary1.Models;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public AdminController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
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

        // ✅ 2. מחיקת משתמש לפי ID (כולל תמונות, בגדים, אאוטפיטים, מועדפים)
        [HttpDelete("user/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // מחיקת אאוטפיטים
                var deleteOutfits = @"
                    DELETE FROM outfititems WHERE OutfitID IN (SELECT OutfitID FROM outfits WHERE UserID = @UserID);
                    DELETE FROM outfits WHERE UserID = @UserID;";
                using var cmd1 = new MySqlCommand(deleteOutfits, connection, (MySqlTransaction)transaction);
                cmd1.Parameters.AddWithValue("@UserID", userId);
                await cmd1.ExecuteNonQueryAsync();

                // מחיקת תמונות של הבגדים של המשתמש
                var deleteImages = "DELETE FROM images WHERE OwnerID IN (SELECT ItemID FROM clothingitems WHERE UserID = @UserID)";
                using var cmd2 = new MySqlCommand(deleteImages, connection, (MySqlTransaction)transaction);
                cmd2.Parameters.AddWithValue("@UserID", userId);
                await cmd2.ExecuteNonQueryAsync();

                // מחיקת בגדים
                var deleteClothes = "DELETE FROM clothingitems WHERE UserID = @UserID";
                using var cmd3 = new MySqlCommand(deleteClothes, connection, (MySqlTransaction)transaction);
                cmd3.Parameters.AddWithValue("@UserID", userId);
                await cmd3.ExecuteNonQueryAsync();

                // מחיקת מועדפים
                var deleteFavorites = "DELETE FROM favorites WHERE UserID = @UserID";
                using var cmd4 = new MySqlCommand(deleteFavorites, connection, (MySqlTransaction)transaction);
                cmd4.Parameters.AddWithValue("@UserID", userId);
                await cmd4.ExecuteNonQueryAsync();

                // מחיקת המשתמש עצמו
                var deleteUser = "DELETE FROM users WHERE UserID = @UserID";
                using var cmd5 = new MySqlCommand(deleteUser, connection, (MySqlTransaction)transaction);
                cmd5.Parameters.AddWithValue("@UserID", userId);
                await cmd5.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                return Ok("המשתמש נמחק בהצלחה.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { Message = "❌ שגיאה במחיקת המשתמש", Error = ex.Message });
            }
        }

        // ✅ 3. החזרת כמות משתמשים שאינם אדמין
        [HttpGet("non-admin-count")]
        public async Task<IActionResult> GetNonAdminUsersCount()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM users WHERE Role != 'Admin'";
            using var command = new MySqlCommand(query, connection);
            var count = Convert.ToInt32(await command.ExecuteScalarAsync());

            return Ok(new { NonAdminUsersCount = count });
        }

        [HttpGet("general")]
        public async Task<IActionResult> GetGeneralStats()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                int totalUsers = 0;
                int totalClothingItems = 0;
                int totalOutfits = 0;

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // ספירת משתמשים
                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM users", connection))
                {
                    totalUsers = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // ספירת פריטי לבוש
                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM clothingitems", connection))
                {
                    totalClothingItems = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // ספירת אאוטפיטים
                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM outfits", connection))
                {
                    totalOutfits = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                return Ok(new
                {
                    totalUsers,
                    totalClothingItems,
                    totalOutfits
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "שגיאה בטעינת נתוני סטטיסטיקה", Error = ex.Message });
            }
        }

        [HttpGet("most-popular-item")]
        public async Task<ActionResult<PopularItem>> GetMostPopularItem()
        {
            var query = @"
        SELECT Category, ColorName, COUNT(*) AS ItemCount
        FROM clothingitems
        GROUP BY Category, ColorName
        ORDER BY ItemCount DESC
        LIMIT 1;
    ";

            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var cmd = new MySqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var item = new PopularItem
                {
                    Category = reader.GetString("Category"),
                    ColorName = reader.GetString("ColorName"),
                    ItemCount = reader.GetInt32("ItemCount")
                };

                return Ok(item);
            }

            return NotFound();
        }

    }
}



   

