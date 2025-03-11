using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;
using ClassLibrary1.Models; // שימוש במחלקות מה-Class Library

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
    public async Task<IActionResult> AddOutfit([FromBody] Outfit outfit)
    {
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
                        // 1️⃣ שמירת האאוטפיט בטבלת `Outfits`
                        string queryOutfit = @"
                        INSERT INTO Outfits (UserID, Name, DateCreated, EventID)
                        VALUES (@UserID, @Name, @DateCreated, @EventID);
                        SELECT LAST_INSERT_ID();";

                        int outfitId;
                        using (var command = new MySqlCommand(queryOutfit, connection, (MySqlTransaction)transaction))
                        {
                            command.Parameters.AddWithValue("@UserID", outfit.UserID);
                            command.Parameters.AddWithValue("@Name", outfit.Name);
                            command.Parameters.AddWithValue("@DateCreated", outfit.DateCreated ?? DateTime.UtcNow);
                            command.Parameters.AddWithValue("@EventID", outfit.EventID);

                            outfitId = Convert.ToInt32(await command.ExecuteScalarAsync());
                        }

                        // 2️⃣ שמירת פריטי הלבוש בטבלת `OutfitItems`
                        foreach (var clothingItemId in outfit.OutfitItems.Select(o => o.ItemID))
                        {
                            string queryOutfitItem = @"
                            INSERT INTO OutfitItems (OutfitID, ItemID)
                            VALUES (@OutfitID, @ItemID);";

                            using (var command = new MySqlCommand(queryOutfitItem, connection, (MySqlTransaction)transaction))
                            {
                                command.Parameters.AddWithValue("@OutfitID", outfitId);
                                command.Parameters.AddWithValue("@ItemID", clothingItemId);
                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        await transaction.CommitAsync();
                        return Ok("Outfit saved successfully!");
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        return StatusCode(500, "Error saving outfit");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

}
