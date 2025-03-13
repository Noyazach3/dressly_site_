using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using ClassLibrary1.Models;

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
                            // שמירת האאוטפיט בטבלת Outfits
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

                            // שמירת מזהי פריטי הלבוש בטבלת OutfitItems
                            foreach (var clothingItemId in outfit.ClothingItemIDs)
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

        [HttpGet("get-user-outfits")]
        public async Task<IActionResult> GetUserOutfits([FromQuery] int userId)
        {
            string connectionString = _config.GetConnectionString("DefaultConnection");
            var outfits = new List<Outfit>();

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                    SELECT OutfitID, UserID, Name, DateCreated, EventID
                    FROM outfits
                    WHERE UserID = @UserID;";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var outfit = new Outfit
                                {
                                    OutfitID = reader.GetInt32(reader.GetOrdinal("OutfitID")),
                                    UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                    Name = reader.GetString(reader.GetOrdinal("Name")),
                                    DateCreated = reader.GetDateTime(reader.GetOrdinal("DateCreated")),
                                    EventID = reader.IsDBNull(reader.GetOrdinal("EventID")) ? 0 : reader.GetInt32(reader.GetOrdinal("EventID")),
                                    ClothingItemIDs = new List<int>()
                                };

                                outfits.Add(outfit);
                            }
                        }
                    }

                    // טעינת פריטי הלבוש לכל אאוטפיט
                    foreach (var outfit in outfits)
                    {
                        using (var cmdItems = new MySqlCommand("SELECT ItemID FROM outfititems WHERE OutfitID = @OutfitID", connection))
                        {
                            cmdItems.Parameters.AddWithValue("@OutfitID", outfit.OutfitID);
                            using (var readerItems = await cmdItems.ExecuteReaderAsync())
                            {
                                while (await readerItems.ReadAsync())
                                {
                                    int itemIdOrdinal = readerItems.GetOrdinal("ItemID");
                                    outfit.ClothingItemIDs.Add(readerItems.GetInt32(itemIdOrdinal));
                                }
                            }
                        }
                    }

                    return Ok(outfits);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}
