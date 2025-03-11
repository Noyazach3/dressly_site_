using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ClassLibrary1.Models;

[Route("api/[controller]")]
[ApiController]
public class ClothingItemsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ClothingItemsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // GET: api/clothingitems
    // שליפת כל פריטי הלבוש
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
                string query = "SELECT * FROM clothingitems";

                using (var command = new MySqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var item = new ClothingItem
                        {
                            ItemID = reader.GetInt32("ItemID"),
                            Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? string.Empty : reader.GetString("Category"),
                            Season = reader.IsDBNull(reader.GetOrdinal("Season")) ? string.Empty : reader.GetString("Season"),
                            UsageType = reader.IsDBNull(reader.GetOrdinal("UsageType")) ? string.Empty : reader.GetString("UsageType"),
                            Color = reader.IsDBNull(reader.GetOrdinal("ColorName"))
                                ? new ClassLibrary1.Models.Color()
                                : new ClassLibrary1.Models.Color { ColorName = reader.GetString("ColorName") },
                            ImageData = reader.IsDBNull(reader.GetOrdinal("ImageData")) ? null : (byte[])reader["ImageData"],
                            WashAfterUses = reader.IsDBNull(reader.GetOrdinal("WashAfterUses")) ? 1 : reader.GetInt32("WashAfterUses"),
                            DateAdded = reader.IsDBNull(reader.GetOrdinal("DateAdded")) ? (DateTime?)null : reader.GetDateTime("DateAdded"),
                            LastWornDate = reader.IsDBNull(reader.GetOrdinal("LastWornDate")) ? (DateTime?)null : reader.GetDateTime("LastWornDate"),
                            IsWashed = reader.IsDBNull(reader.GetOrdinal("IsWashed")) ? false : reader.GetInt32("IsWashed") == 1
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


    // POST: api/clothingitems
    // הוספת פריט לבוש חדש
    [HttpPost("add")]
    public async Task<IActionResult> AddClothingItem([FromForm] int UserID, [FromForm] string Category, [FromForm] int ColorID,
                                                    [FromForm] string Season, [FromForm] IFormFile Image, [FromForm] int WashAfterUses,
                                                    [FromForm] string UsageType, [FromForm] bool IsWashed)
    {
        string connectionString = _configuration.GetConnectionString("DefaultConnection");

        try
        {
            byte[] imageData = null;
            if (Image != null)
            {
                using (var ms = new MemoryStream())
                {
                    await Image.CopyToAsync(ms);
                    imageData = ms.ToArray();
                }
            }

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = @"
                INSERT INTO clothingitems (UserID, Category, ColorID, Season, ImageData, DateAdded, WashAfterUses, UsageType, IsWashed)
                VALUES (@UserID, @Category, @ColorID, @Season, @ImageData, @DateAdded, @WashAfterUses, @UsageType, @IsWashed)";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", UserID);
                    command.Parameters.AddWithValue("@Category", Category);
                    command.Parameters.AddWithValue("@ColorID", ColorID);
                    command.Parameters.AddWithValue("@Season", Season);
                    command.Parameters.AddWithValue("@ImageData", imageData);
                    command.Parameters.AddWithValue("@DateAdded", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@WashAfterUses", WashAfterUses);
                    command.Parameters.AddWithValue("@UsageType", UsageType);
                    command.Parameters.AddWithValue("@IsWashed", IsWashed ? 1 : 0);

                    await command.ExecuteNonQueryAsync();
                }
            }
            return Ok("Clothing item added successfully");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error adding clothing item", Error = ex.Message });
        }
    }

    [HttpGet("get-image/{itemId}")]
    public async Task<IActionResult> GetImage(int itemId)
    {
        string connectionString = _configuration.GetConnectionString("DefaultConnection");
        byte[] imageData = null;

        using (var connection = new MySqlConnection(connectionString))
        {
            await connection.OpenAsync();
            string query = "SELECT ImageData FROM clothingitems WHERE ItemID = @ItemID";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ItemID", itemId);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        imageData = reader["ImageData"] as byte[];
                    }
                }
            }
        }

        if (imageData == null || imageData.Length == 0)
            return NotFound("Image not found.");

        return File(imageData, "image/jpeg"); // מחזיר את התמונה בפורמט מתאים
    }


    // PUT: api/clothingitems/{id}
    // עדכון פריט לבוש קיים
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClothingItem(int id, [FromBody] ClothingItem item)
    {
        string connectionString = _configuration.GetConnectionString("DefaultConnection");

        try
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = @"
                UPDATE clothingitems
                SET Category = @Category,
                    Season = @Season,
                    UsageType = @UsageType,
                    ImageData = @ImageData,
                    WashAfterUses = @WashAfterUses,
                    DateAdded = @DateAdded,
                    IsWashed = @IsWashed
                WHERE ItemID = @ItemID";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Category", item.Category);
                    command.Parameters.AddWithValue("@Season", item.Season);
                    command.Parameters.AddWithValue("@UsageType", item.UsageType);
                    command.Parameters.AddWithValue("@ImageData", item.ImageData);
                    command.Parameters.AddWithValue("@WashAfterUses", item.WashAfterUses);
                    command.Parameters.AddWithValue("@DateAdded", item.DateAdded);
                    command.Parameters.AddWithValue("@IsWashed", item.IsWashed ? 1 : 0);
                    command.Parameters.AddWithValue("@ItemID", id);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0 ? Ok("Clothing item updated successfully") : NotFound("Clothing item not found");
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error updating clothing item", Error = ex.Message });
        }
    }
}
