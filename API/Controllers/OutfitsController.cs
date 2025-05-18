using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using ClassLibrary1.Models;
using ClassLibrary1.DTOs;
using System.Data;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OutfitsController : ControllerBase
    {
        private readonly IConfiguration _config;

        public OutfitsController(IConfiguration configuration)
        {
            _config = configuration;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddOutfit([FromBody] OutfitSaveDto outfit)
        {

            if (outfit.UserID == null)
            {
                return BadRequest(new { Message = "UserID is missing from request body!" });
            }


            string connectionString = _config.GetConnectionString("DefaultConnection");

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            string insertQuery = @"INSERT INTO outfits (`UserID`, `Name`, `DateCreated`)
                                                 VALUES (@UserID, @Name, @DateCreated);"; // שאילתה להוספת אאוטפיט לטבלת האאוטפיטים


                            using (var insertCommand = new MySqlCommand(insertQuery, connection, (MySqlTransaction)transaction))
                            {
                                // הוספת הפרמטרים
                                insertCommand.Parameters.AddWithValue("@UserID", outfit.UserID);
                                insertCommand.Parameters.AddWithValue("@Name", outfit.Name);
                                insertCommand.Parameters.AddWithValue("@DateCreated", outfit.DateCreated ?? DateTime.UtcNow.Date);

                                Console.WriteLine(" פרמטרים שנשלחים ל־MySQL:");
                                foreach (MySqlParameter p in insertCommand.Parameters)
                                {
                                    Console.WriteLine($"{p.ParameterName} = {p.Value}");
                                }

                                Console.WriteLine(" מנסה לבצע ExecuteNonQuery...");
                                
                                
                                try // ביצוע השאילתה
                                {
                                    await insertCommand.ExecuteNonQueryAsync();
                                    Console.WriteLine(" השאילתה בוצעה בהצלחה!");

                                    // בדיקה מיידית – שליפת כל האאוטפיטים הקיימים
                                    using (var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM outfits", connection, (MySqlTransaction)transaction))
                                    {
                                        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                                        Console.WriteLine($" כמות האאוטפיטים בטבלה (לפני Commit): {count}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($" שגיאה בהפעלת ExecuteNonQuery: {ex.Message}");
                                }
                            }

                            int outfitId; //שליפת המזהה של האוטפיט החדש שנוצר
                            using (var idCommand = new MySqlCommand("SELECT LAST_INSERT_ID();", connection, (MySqlTransaction)transaction))
                            {
                                outfitId = Convert.ToInt32(await idCommand.ExecuteScalarAsync());
                            }

                            foreach (var clothingItemId in outfit.ClothingItemIDs) // הוספת הקשרים לטבלת outfititems עבור כל פריט לבוש
                            {
                                string queryOutfitItem = @"INSERT INTO OutfitItems (OutfitID, ItemID)
                                                   VALUES (@OutfitID, @ItemID);";

                                using (var command = new MySqlCommand(queryOutfitItem, connection, (MySqlTransaction)transaction))
                                {
                                    command.Parameters.AddWithValue("@OutfitID", outfitId);
                                    command.Parameters.AddWithValue("@ItemID", clothingItemId);
                                    await command.ExecuteNonQueryAsync();
                                }
                            }

                            await transaction.CommitAsync();
                            Console.WriteLine(" האאוטפיט נשמר בהצלחה במסד הנתונים");
                            return Ok("Outfit saved successfully!");
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            Console.WriteLine($" שגיאה פנימית בשמירת האאוטפיט: {ex.Message}");
                            return StatusCode(500, new { Message = "Error saving outfit", Error = ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❗ שגיאה בפתיחת חיבור ל־DB: {ex.Message}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }




        [HttpGet("get-user-outfits")] // שליפת כל האאוטפיטים של משתמש
        public async Task<IActionResult> GetUserOutfits([FromQuery] int userId)
        {
            string connectionString = _config.GetConnectionString("DefaultConnection");
            var outfits = new List<Outfit>(); // רשימה ריקה שתכיל את כל האאוטפיטים

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                                   SELECT OutfitID, UserID, Name, DateCreated
                                   FROM outfits
                                   WHERE UserID = @UserID;"; // שאילתה שמחזירה את כל האאוטפיטים של משתמש

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var outfit = new Outfit
                                {
                                    OutfitID = reader.GetInt32("OutfitID"), //מזהה האוטפיט
                                    UserID = reader.GetInt32("UserID"), //מזהה משתמש
                                    Name = reader.GetString("Name"), // שם האאוטפיט
                                    DateCreated = reader.IsDBNull(reader.GetOrdinal("DateCreated")) //תאריך יצירה
                                        ? DateTime.MinValue
                                        : reader.GetDateTime("DateCreated"),
                                    ClothingItemIDs = new List<int>() // רשימת פרטי הלבוש שיתווספו בהמשך
                                };

                                outfits.Add(outfit);
                            }
                        }
                    }

                    // לכל אאוטפיט נוסיף את רשימת הפריטים מתוך הטבלה
                    foreach (var outfit in outfits)
                    {
                        using (var cmdItems = new MySqlCommand("SELECT ItemID FROM outfititems WHERE OutfitID = @OutfitID", connection))
                        {
                            cmdItems.Parameters.AddWithValue("@OutfitID", outfit.OutfitID);
                            using (var readerItems = await cmdItems.ExecuteReaderAsync())
                            {
                                while (await readerItems.ReadAsync())
                                {
                                    outfit.ClothingItemIDs.Add(readerItems.GetInt32("ItemID"));
                                }
                            }
                        }
                    }

                    return Ok(outfits); // החזרת רשימת האאוטפיטים
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ שגיאה בטעינת האאוטפיטים: {ex.Message}");
                return StatusCode(500, new { Message = "Error loading outfits", Error = ex.Message });
            }
        }

        [HttpPost("add-favorite")] // הוספת אאוטפיט למועדפים
        public async Task<IActionResult> AddFavorite([FromBody] FavoriteDto dto)
        {
            var connectionString = _config.GetConnectionString("DefaultConnection");

            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var checkQuery = "SELECT COUNT(*) FROM favorites WHERE UserID = @UserID AND OutfitID = @OutfitID"; //בדיקה אם האאוטפיט כבר נמצא במועדפים של המשתמש
                using var checkCmd = new MySqlCommand(checkQuery, connection);
                checkCmd.Parameters.AddWithValue("@UserID", dto.UserID);
                checkCmd.Parameters.AddWithValue("@OutfitID", dto.OutfitID);

                var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                if (exists)
                    return Ok("Already favorited"); // אם כבר מועדף – לא מוסיף שוב


                var insertQuery = "INSERT INTO favorites (UserID, OutfitID) VALUES (@UserID, @OutfitID)"; // הוספה לטבלת favorites אם עדיין לא קיים
                using var insertCmd = new MySqlCommand(insertQuery, connection);
                insertCmd.Parameters.AddWithValue("@UserID", dto.UserID);
                insertCmd.Parameters.AddWithValue("@OutfitID", dto.OutfitID);

                await insertCmd.ExecuteNonQueryAsync(); // ביצוע ההוספה בפועל
                return Ok("Added to favorites");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error adding favorite", Error = ex.Message });
            }
        }

        [HttpGet("get-favorites/{userId}")] // שליפת כל המועדפים עבור משתמש 
        public async Task<IActionResult> GetFavorites(int userId)
        {
            var connectionString = _config.GetConnectionString("DefaultConnection");
            var favoriteIds = new List<int>(); // יצירת רשימה ריקה לשמירת מזהי האאוטפיטים

            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = "SELECT OutfitID FROM favorites WHERE UserID = @UserID"; // שאילתה שמחזירה את כל האאוטפיטים שסומנו כמועדפים ע"י המשתמש
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserID", userId); // הוספת מזהה המשתמש לשאילתה

                using var reader = await cmd.ExecuteReaderAsync(); // הרצת השאילתה וקריאת התוצאות שורה־שורה
                while (await reader.ReadAsync())
                {
                    favoriteIds.Add(reader.GetInt32(0)); // הוספת כל מזהה אאוטפיט לרשימה
                }

                return Ok(favoriteIds);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error loading favorites", Error = ex.Message });
            }
        }



        [HttpDelete("delete/{outfitId}")] // מחיקת אאוטפיט
        public async Task<IActionResult> DeleteOutfit(int outfitId)
        {
            var connectionString = _config.GetConnectionString("DefaultConnection");

            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync(); // שכל השלבים יתבצעו יחד

                try
                {
                    // שלב 1: מחיקת הקשרים מטבלת OutfitItems
                    string deleteItemsQuery = "DELETE FROM outfititems WHERE OutfitID = @OutfitID";
                    using (var cmd1 = new MySqlCommand(deleteItemsQuery, connection, (MySqlTransaction)transaction))
                    {
                        cmd1.Parameters.AddWithValue("@OutfitID", outfitId);
                        await cmd1.ExecuteNonQueryAsync();
                    }

                    // שלב 2: מחיקת רשומה מטבלת Outfits
                    string deleteOutfitQuery = "DELETE FROM outfits WHERE OutfitID = @OutfitID";
                    using (var cmd2 = new MySqlCommand(deleteOutfitQuery, connection, (MySqlTransaction)transaction))
                    {
                        cmd2.Parameters.AddWithValue("@OutfitID", outfitId);
                        int affectedRows = await cmd2.ExecuteNonQueryAsync();

                        if (affectedRows == 0)
                        {
                            await transaction.RollbackAsync();
                            return NotFound("האאוטפיט לא נמצא.");
                        }
                    }

                    await transaction.CommitAsync();
                    return Ok("האאוטפיט נמחק בהצלחה.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { Message = "שגיאה בעת מחיקת האאוטפיט", Error = ex.Message });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "שגיאה כללית", Error = ex.Message });
            }
        }


    }
}
