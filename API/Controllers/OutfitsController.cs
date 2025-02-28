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

    // POST: api/outfits
    [HttpPost]
    public async Task<IActionResult> AddOutfit([FromBody] Outfit outfit)
    {
        string connectionString = _config.GetConnectionString("DefaultConnection");

        try
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = @"
                    INSERT INTO outfits (OutfitID, UserID, Name, DateCreated, EventID)
                    VALUES (@OutfitID, @UserID, @Name, @DateCreated, @EventID)";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@OutfitID", outfit.OutfitID);
                    command.Parameters.AddWithValue("@UserID", outfit.UserID);
                    command.Parameters.AddWithValue("@Name", outfit.Name);
                    command.Parameters.AddWithValue("@DateCreated", outfit.DateCreated);
                    command.Parameters.AddWithValue("@EventID", outfit.EventID);
                    await command.ExecuteNonQueryAsync();
                }
            }
            return Ok("Outfit added successfully");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error adding outfit", Error = ex.Message });
        }
    }
}
