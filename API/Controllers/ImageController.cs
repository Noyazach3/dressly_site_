using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ImageController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetImage(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                string query = "SELECT ImageData, ImageType FROM images WHERE ImageID = @ImageID";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ImageID", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    byte[] imageData = (byte[])reader["ImageData"];
                    string imageType = reader.IsDBNull(reader.GetOrdinal("ImageType")) ? "image/jpeg" : reader.GetString(reader.GetOrdinal("ImageType"));

                    return File(imageData, imageType);
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error retrieving image", Error = ex.Message });
            }
        }


    }
}
