using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ClassLibrary1.Models;
using System.IO;
using ClassLibrary1.DTOs;


namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClothingItemsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        //(appsettings.json) כדי להתחבר למסד
        public ClothingItemsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet] //שליפת כל פרטי הלבוש
        public async Task<IActionResult> GetClothingItems()
        {
            var items = new List<ClothingItem>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                           SELECT c.ItemID, c.UserID, c.Category, c.Season, c.UsageType, c.ColorName, c.ImageID 
                           FROM clothingitems c"; // שאילתת SQL לשליפת נתוני הבגדים

                    using (var command = new MySqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync()) //יצירת אובייקט
                        {
                            var item = new ClothingItem
                            {
                                ItemID = reader.GetInt32(reader.GetOrdinal("ItemID")),
                                UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? null : reader.GetString(reader.GetOrdinal("Category")),
                                Season = reader.IsDBNull(reader.GetOrdinal("Season")) ? null : reader.GetString(reader.GetOrdinal("Season")),
                                UsageType = reader.IsDBNull(reader.GetOrdinal("UsageType")) ? null : reader.GetString(reader.GetOrdinal("UsageType")),
                                Color = new Color
                                {
                                    ColorName = reader.IsDBNull(reader.GetOrdinal("ColorName")) ? null : reader.GetString(reader.GetOrdinal("ColorName"))
                                },
                                ImageID = reader.IsDBNull(reader.GetOrdinal("ImageID")) ? null : reader.GetInt32(reader.GetOrdinal("ImageID"))
                            };

                            items.Add(item);
                        }
                    }
                }

                return Ok(items); //מחזיר את רשימת הבגדים לבלייזור
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error retrieving all clothing items", Error = ex.Message });
            }
        }

        
        [HttpGet("user/{userId}")] //שליפת כל פרטי הלבוש של משתמש ספציפי
        public async Task<IActionResult> GetClothingItemsForUser(int userId)
        {
            var items = new List<ClothingItem>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                           SELECT c.ItemID, c.Category, c.Season, c.UsageType, c.ColorName, c.ImageID
                           FROM clothingitems c
                           WHERE c.UserID = @UserID"; // שאילתת SQL לשליפת נתוני הבגדים

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserID", userId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var item = new ClothingItem //יצירת אובייקט
                                {
                                    ItemID = reader.GetInt32(reader.GetOrdinal("ItemID")),
                                    Category = reader.GetString(reader.GetOrdinal("Category")),
                                    Season = reader.GetString(reader.GetOrdinal("Season")),
                                    UsageType = reader.GetString(reader.GetOrdinal("UsageType")),
                                    Color = new Color
                                    {
                                        ColorName = reader.GetString(reader.GetOrdinal("ColorName"))
                                    },
                                    ImageID = reader.IsDBNull(reader.GetOrdinal("ImageID")) ? null : reader.GetInt32(reader.GetOrdinal("ImageID"))
                                };

                                items.Add(item);
                            }
                        }
                    }
                }

                return Ok(items); // מחזיר את הבגדים של המשתמש
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error retrieving clothing items for user", Error = ex.Message });
            }
        }






        [HttpPost("addClothingItemAttributes")] //הוספת פריט לבוש (ללא תמונה)נ
        public async Task<IActionResult> AddClothingItemAttributes([FromBody] ClothingItemDto itemDto)
        {
            Console.WriteLine("🔥 start addClothingItemAttributes");

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                itemDto.DateAdded = DateTime.Now;

                int newItemId;
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string insertQuery = @"
                         INSERT INTO clothingitems (UserID, Category, ColorName, Season, DateAdded, UsageType)
                         VALUES (@UserID, @Category, @ColorName, @Season, @DateAdded, @UsageType);
                         SELECT LAST_INSERT_ID();"; // שאילתת SQL היוצרת פריט לבוש חדש

                    using (var command = new MySqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@UserID", itemDto.UserID);
                        command.Parameters.AddWithValue("@Category", itemDto.Category);
                        command.Parameters.AddWithValue("@ColorName", itemDto.ColorName);
                        command.Parameters.AddWithValue("@Season", itemDto.Season);
                        command.Parameters.AddWithValue("@DateAdded", itemDto.DateAdded);
                        command.Parameters.AddWithValue("@UsageType", itemDto.UsageType);

                        newItemId = Convert.ToInt32(await command.ExecuteScalarAsync());
                    }
                }

                return Ok(newItemId); // מחזיר את ה־ID של הפריט החדש 
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ שגיאה ב-SQL: " + ex.Message);
                return StatusCode(500, new { Message = "Error adding clothing item", Error = ex.Message });
            }
        }


        [HttpPost("uploadImageForItem")] //העלאת תמונה לפריט קיים בעזרת ImageUploadDto
        public async Task<IActionResult> UploadImageForItem([FromForm] ImageUploadDto dto)
        {
            Console.WriteLine("📸 קיבלנו בקשה להעלאת תמונה לפריט " + dto.ItemID);

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                // הפיכת הקובץ ל־byte[]
                using var memoryStream = new MemoryStream();
                await dto.ImageFile.CopyToAsync(memoryStream);
                byte[] imageData = memoryStream.ToArray();

                // שמירה לטבלת image
                int imageId;
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string insertImageQuery = @"
                INSERT INTO images  (OwnerID, ImageData, ImageType, UploadDate)
                VALUES (@OwnerID, @ImageData, @ImageType, @UploadDate);
                SELECT LAST_INSERT_ID();";

                    using (var command = new MySqlCommand(insertImageQuery, connection))
                    {
                        command.Parameters.AddWithValue("@OwnerID", dto.ItemID);
                        command.Parameters.AddWithValue("@ImageData", imageData);
                        command.Parameters.AddWithValue("@ImageType", dto.ImageFile.ContentType);
                        command.Parameters.AddWithValue("@UploadDate", DateTime.Now);

                        imageId = Convert.ToInt32(await command.ExecuteScalarAsync());
                    }

                    // עדכון הטבלה clothingitems עם מזהה התמונה
                    string updateItemQuery = "UPDATE clothingitems SET ImageID = @ImageID WHERE ItemID = @ItemID";
                    using (var command = new MySqlCommand(updateItemQuery, connection))
                    {
                        command.Parameters.AddWithValue("@ImageID", imageId);
                        command.Parameters.AddWithValue("@ItemID", dto.ItemID);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { Message = "Images uploaded", ImageID = imageId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error uploading image", Error = ex.Message });
            }
        }


        // פעולת עזר שנועדה להמיר קובץ (למשל תמונה) שמתקבל מהמשתמש
        // (IFormFile), לתוך מערך של בייטים
        // (byte[]) – כדי שנוכל לשמור אותו במסד הנתונים.
        private async Task<byte[]> ConvertToByteArray(IFormFile file)
        {
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }



        

        //  הוספת פריט לבוש למועדפים
        [HttpPost("add-favorite")]
        public async Task<IActionResult> AddFavoriteItem([FromBody] FavoriteDto dto)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using var connection = new MySqlConnection(connStr);
                await connection.OpenAsync();

                string query = @"INSERT INTO favorites (UserID, ItemID) VALUES (@UserID, @ItemID)";//שאילתה להוספת שורה לטבלת מעודפים עם מזהה המשתמש ומזהה הפריט
                using var cmd = new MySqlCommand(query, connection); //יצירת הפקודה עם השאילתה והוספת הפרמטרים בצורה מאבטחת
                cmd.Parameters.AddWithValue("@UserID", dto.UserID);//מזהה המשתמש שמוסיף מועדף 
                cmd.Parameters.AddWithValue("@ItemID", dto.ItemID);// מזהה פריט הלבוש

                await cmd.ExecuteNonQueryAsync();// ביצוע השאילתה בפועל
                return Ok(new { Message = "Item marked as favorite" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error adding favorite", Error = ex.Message });
            }
        }

        //  הסרת פריט לבוש ממועדפים
        [HttpDelete("remove-favorite")]
        public async Task<IActionResult> RemoveFavoriteItem([FromQuery] int userId, [FromQuery] int itemId)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using var connection = new MySqlConnection(connStr);
                await connection.OpenAsync();

                string query = @"DELETE FROM favorites WHERE UserID = @UserID AND ItemID = @ItemID"; // שאילתה שמוחקת שורה מהטבלת מועדפים
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserID", userId); // המשתמש שהסיר את הפריט
                cmd.Parameters.AddWithValue("@ItemID", itemId); // הפריט שהוסר מהמועדפים

                await cmd.ExecuteNonQueryAsync();// ביצוע המחיקה בפועל
                return Ok(new { Message = "Item removed from favorites" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error removing favorite", Error = ex.Message });
            }
        }


        [HttpGet("get-favorites/{userId}")] //קבלת כל הפריטים המועדפים לפי המשתמש
        public async Task<IActionResult> GetFavoriteItemIDs(int userId)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            var favoriteItemIDs = new List<int>(); //רשימה ריקה שתכיל את כל הפריטים המועדפים

            try
            {
                using var connection = new MySqlConnection(connStr);
                await connection.OpenAsync();

                string query = @"SELECT ItemID FROM favorites WHERE UserID = @UserID AND ItemID IS NOT NULL"; //שאילתה המחזירה את מזהי הפריטים המועדפים של המשתמש
                using var cmd = new MySqlCommand(query, connection); //יצירת הפקודה עם הפרמטרים כדי למנוע SQL Injection
                cmd.Parameters.AddWithValue("@UserID", userId); // מזהה המשתמש

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    favoriteItemIDs.Add(reader.GetInt32(0)); //הוספה לרשימה
                }

                return Ok(favoriteItemIDs); // החזרת הרשימה
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error retrieving favorite items", Error = ex.Message });
            }
        }



        [HttpDelete("{itemId}")] //מחיקת פריט לבוש 
        public async Task<IActionResult> DeleteClothingItem(int itemId)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using var connection = new MySqlConnection(connStr);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    //  שלב 1: ניתוק התמונה מהפריט (ImageID = NULL)
                    var nullifyImageQuery = "UPDATE clothingitems SET ImageID = NULL WHERE ItemID = @ItemID";
                    using (var cmd = new MySqlCommand(nullifyImageQuery, connection, (MySqlTransaction)transaction))
                    {
                        cmd.Parameters.AddWithValue("@ItemID", itemId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    //  שלב 2: מחיקה מטבלת outfititems
                    var deleteOutfitItemsQuery = "DELETE FROM outfititems WHERE ItemID = @ItemID";
                    using (var cmd = new MySqlCommand(deleteOutfitItemsQuery, connection, (MySqlTransaction)transaction))
                    {
                        cmd.Parameters.AddWithValue("@ItemID", itemId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    //  שלב 3: מחיקת התמונה מהטבלה images לפי OwnerID
                    var deleteImageQuery = "DELETE FROM images WHERE OwnerID = @ItemID";
                    using (var cmd = new MySqlCommand(deleteImageQuery, connection, (MySqlTransaction)transaction))
                    {
                        cmd.Parameters.AddWithValue("@ItemID", itemId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    //  שלב 4: מחיקת הפריט עצמו
                    var deleteItemQuery = "DELETE FROM clothingitems WHERE ItemID = @ItemID";
                    using (var cmd = new MySqlCommand(deleteItemQuery, connection, (MySqlTransaction)transaction))
                    {
                        cmd.Parameters.AddWithValue("@ItemID", itemId);
                        var rowsAffected = await cmd.ExecuteNonQueryAsync();

                        if (rowsAffected == 0) // אם לא נמחקה אף שורה אין פריט כזה
                        {
                            await transaction.RollbackAsync();
                            return NotFound(new { Message = "❌ פריט הלבוש לא נמצא" });
                        }
                    }

                    await transaction.CommitAsync();
                    return Ok(new { Message = "✅ הפריט נמחק עם כל התמונה והקשרים" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { Message = "❌ שגיאה במהלך המחיקה", Error = ex.Message });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "❌ שגיאה בפתיחת החיבור למסד", Error = ex.Message });
            }
        }



    }



}


        