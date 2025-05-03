using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ClassLibrary1.Models;
using System.IO;


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
                                WashAfterUses = reader.IsDBNull(reader.GetOrdinal("WashAfterUses")) ? 1 : reader.GetInt32(reader.GetOrdinal("WashAfterUses")),
                                DateAdded = reader.IsDBNull(reader.GetOrdinal("DateAdded")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DateAdded")),
                                LastWornDate = reader.IsDBNull(reader.GetOrdinal("LastWornDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("LastWornDate")),
                                IsWashed = reader.IsDBNull(reader.GetOrdinal("IsWashed")) ? false : reader.GetInt32(reader.GetOrdinal("IsWashed")) == 1
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




        [HttpPost("addClothingItem")]
        public async Task<IActionResult> AddClothingItem([FromForm] ClothingItem item, IFormFile imageFile)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                // 1. עדכון שדות:
                item.ColorID = await GetColorIdFromColorName(item.ColorName, connectionString);
                item.DateAdded = DateTime.Now; // עדכון אוטומטי של תאריך הוספה
                item.IsWashed = false; // ברירת מחדל לשדה IsWashed

                // 2. הוספת הפריט לבסיס הנתונים
                int newItemId;
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string insertQuery = @"
                INSERT INTO clothingitems (UserID, Category, ColorID, Season, DateAdded, WashAfterUses, UsageType, IsWashed)
                VALUES (@UserID, @Category, @ColorID, @Season, @DateAdded, @WashAfterUses, @UsageType, @IsWashed);
                SELECT LAST_INSERT_ID();"; // קבלת ה-ItemID שנוצר
                    using (var command = new MySqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@UserID", item.UserID);
                        command.Parameters.AddWithValue("@Category", item.Category);
                        command.Parameters.AddWithValue("@ColorID", item.ColorID);
                        command.Parameters.AddWithValue("@Season", item.Season);
                        command.Parameters.AddWithValue("@DateAdded", item.DateAdded);
                        command.Parameters.AddWithValue("@WashAfterUses", item.WashAfterUses);
                        command.Parameters.AddWithValue("@UsageType", item.UsageType);
                        command.Parameters.AddWithValue("@IsWashed", item.IsWashed ? 1 : 0);

                        newItemId = Convert.ToInt32(await command.ExecuteScalarAsync()); // נשמור את ה-ItemID
                    }
                }

                // 3. אם נבחרה תמונה, מעלים אותה ומקבלים ImageID
                int imageId = 0;
                if (imageFile != null && imageFile.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await imageFile.CopyToAsync(memoryStream);
                        var image = new Image
                        {
                            OwnerID = newItemId, // הפריט שיצרנו
                            ImageData = memoryStream.ToArray(),
                            UploadDate = DateTime.Now,
                            ImageType = imageFile.ContentType
                        };

                        imageId = await UploadImage(image); // שמירת התמונה בטבלה נפרדת
                    }

                    // 4. עדכון הפריט עם ImageID
                    using (var connection = new MySqlConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        string updateQuery = "UPDATE clothingitems SET ImageID = @ImageID WHERE ItemID = @ItemID";
                        using (var command = new MySqlCommand(updateQuery, connection))
                        {
                            command.Parameters.AddWithValue("@ImageID", imageId);
                            command.Parameters.AddWithValue("@ItemID", newItemId); // עדכון ה-ItemID שנוצר
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }

                return Ok("Clothing item added successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error adding clothing item", Error = ex.Message });
            }
        }






        // עוזרת לקבל את ColorID לפי ColorName
        private async Task<int> GetColorIdFromColorName(string colorName, string connectionString)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT ColorID FROM color WHERE ColorName = @ColorName";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ColorName", colorName);
                    var result = await command.ExecuteScalarAsync();
                    if (result != null)
                    {
                        return Convert.ToInt32(result);
                    }
                    else
                    {
                        throw new Exception("Color not found for the given ColorName.");
                    }
                }
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
                        INSERT INTO image (OwnerID, ImageData, ImageType, UploadDate)
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
    }



}


        