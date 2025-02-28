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
                        items.Add(new ClothingItem
                        {
                            ItemID = reader.GetInt32("ItemID"),
                            Category = reader.GetString("Category"),
                            Season = reader.GetString("Season"),
                            UsageType = reader.GetString("UsageType"),
                            Color = reader.IsDBNull(reader.GetOrdinal("ColorName")) ? new ClassLibrary1.Models.Color() : new ClassLibrary1.Models.Color { ColorName = reader.GetString("ColorName") },
                            ImageURL = reader.GetString("ImageURL"),
                            WashAfterUses = reader.GetInt32("WashAfterUses"),
                            DateAdded = reader.GetDateTime("DateAdded")
                        });
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
    [HttpPost]
    public async Task<IActionResult> AddClothingItem([FromBody] ClothingItem item)
    {
        string connectionString = _configuration.GetConnectionString("DefaultConnection");

        try
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = @"
                    INSERT INTO clothingitems (UserID, Category, ColorID, Season, ImageURL, DateAdded, WashAfterUses, LastWornDate, UsageType)
                    VALUES (@UserID, @Category, @ColorID, @Season, @ImageURL, @DateAdded, @WashAfterUses, NULL, @UsageType)";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", item.UserID);
                    command.Parameters.AddWithValue("@Category", item.Category);
                    command.Parameters.AddWithValue("@ColorID", item.ColorID);
                    command.Parameters.AddWithValue("@Season", item.Season);
                    command.Parameters.AddWithValue("@ImageURL", item.ImageURL);
                    command.Parameters.AddWithValue("@DateAdded", item.DateAdded);
                    command.Parameters.AddWithValue("@WashAfterUses", item.WashAfterUses);
                    command.Parameters.AddWithValue("@UsageType", item.UsageType);
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
                        ImageURL = @ImageURL,
                        WashAfterUses = @WashAfterUses,
                        DateAdded = @DateAdded
                    WHERE ItemID = @ItemID";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Category", item.Category);
                    command.Parameters.AddWithValue("@Season", item.Season);
                    command.Parameters.AddWithValue("@UsageType", item.UsageType);
                    command.Parameters.AddWithValue("@ImageURL", item.ImageURL);
                    command.Parameters.AddWithValue("@WashAfterUses", item.WashAfterUses);
                    command.Parameters.AddWithValue("@DateAdded", item.DateAdded);
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
