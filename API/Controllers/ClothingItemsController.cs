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

        public ClothingItemsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
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
                SELECT c.ItemID, c.UserID, c.Category, c.ColorID, c.Season, c.DateAdded, c.LastWornDate, 
                       c.WashAfterUses, c.UsageType, c.ColorName, c.IsWashed, c.ImageID
                FROM clothingitems c";

                    using (var command = new MySqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = new ClothingItem
                            {
                                ItemID = reader.GetInt32(reader.GetOrdinal("ItemID")),
                                Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? string.Empty : reader.GetString(reader.GetOrdinal("Category")),
                                Season = reader.IsDBNull(reader.GetOrdinal("Season")) ? string.Empty : reader.GetString(reader.GetOrdinal("Season")),
                                UsageType = reader.IsDBNull(reader.GetOrdinal("UsageType")) ? string.Empty : reader.GetString(reader.GetOrdinal("UsageType")),
                                Color = reader.IsDBNull(reader.GetOrdinal("ColorName"))
                                    ? new ClassLibrary1.Models.Color()
                                    : new ClassLibrary1.Models.Color { ColorName = reader.GetString(reader.GetOrdinal("ColorName")) },
                                ImageID = reader.IsDBNull(reader.GetOrdinal("ImageID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("ImageID")),
                                DateAdded = reader.IsDBNull(reader.GetOrdinal("DateAdded")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DateAdded")),
                                LastWornDate = reader.IsDBNull(reader.GetOrdinal("LastWornDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("LastWornDate")),
                            };

                            items.Add(item);
                        }
                    }
                }
                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error retrieving clothing items", Error = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
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
                WHERE c.UserID = @UserID";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserID", userId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var item = new ClothingItem
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

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error retrieving clothing items for user", Error = ex.Message });
            }
        }





        [HttpPost("addClothingItemAttributes")]
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
                SELECT LAST_INSERT_ID();";

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

                return Ok(newItemId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ שגיאה ב-SQL: " + ex.Message);
                return StatusCode(500, new { Message = "Error adding clothing item", Error = ex.Message });
            }
        }


        [HttpPost("uploadImageForItem")]
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


        private async Task<byte[]> ConvertToByteArray(IFormFile file)
        {
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }


        // POST: api/ClothingItems/uploadImage
        [HttpPost("uploadImage")]
        public async Task<int> UploadImage(Image image)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                        INSERT INTO images (OwnerID, ImageData, ImageType, UploadDate)
                        VALUES (@OwnerID, @ImageData, @ImageType, @UploadDate)";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@OwnerID", image.OwnerID);
                        command.Parameters.AddWithValue("@ImageData", image.ImageData);
                        command.Parameters.AddWithValue("@ImageType", image.ImageType);
                        command.Parameters.AddWithValue("@UploadDate", image.UploadDate);
                        await command.ExecuteNonQueryAsync();
                    }

                    // קבלת ה-ImageID של התמונה שהוזנה
                    string selectQuery = "SELECT LAST_INSERT_ID()";
                    using (var command = new MySqlCommand(selectQuery, connection))
                    {
                        return Convert.ToInt32(await command.ExecuteScalarAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error uploading image", ex);
            }
        }

     



        // ✅ הוספת פריט לבוש למועדפים
        [HttpPost("add-favorite")]
        public async Task<IActionResult> AddFavoriteItem([FromBody] FavoriteDto dto)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using var connection = new MySqlConnection(connStr);
                await connection.OpenAsync();

                string query = @"INSERT INTO favorites (UserID, ItemID) VALUES (@UserID, @ItemID)";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserID", dto.UserID);
                cmd.Parameters.AddWithValue("@ItemID", dto.ItemID);

                await cmd.ExecuteNonQueryAsync();
                return Ok(new { Message = "Item marked as favorite" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error adding favorite", Error = ex.Message });
            }
        }

        // ✅ הסרת פריט לבוש ממועדפים
        [HttpDelete("remove-favorite")]
        public async Task<IActionResult> RemoveFavoriteItem([FromQuery] int userId, [FromQuery] int itemId)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using var connection = new MySqlConnection(connStr);
                await connection.OpenAsync();

                string query = @"DELETE FROM favorites WHERE UserID = @UserID AND ItemID = @ItemID";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@ItemID", itemId);

                await cmd.ExecuteNonQueryAsync();
                return Ok(new { Message = "Item removed from favorites" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error removing favorite", Error = ex.Message });
            }
        }

        // ✅ קבלת כל המזהים של פריטי לבוש מועדפים של משתמש
        [HttpGet("get-favorites/{userId}")]
        public async Task<IActionResult> GetFavoriteItemIDs(int userId)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            var favoriteItemIDs = new List<int>();

            try
            {
                using var connection = new MySqlConnection(connStr);
                await connection.OpenAsync();

                string query = @"SELECT ItemID FROM favorites WHERE UserID = @UserID AND ItemID IS NOT NULL";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserID", userId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    favoriteItemIDs.Add(reader.GetInt32(0));
                }

                return Ok(favoriteItemIDs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error retrieving favorite items", Error = ex.Message });
            }
        }


    }



}


        