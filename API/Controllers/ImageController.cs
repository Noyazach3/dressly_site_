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
        
        

        [HttpGet("{id}")] // פעולה שמחזירה תמונה לפי מזהה
        public async Task<IActionResult> GetImage(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // הגדרת השאילתה לשליפת התמונה לפי מזהה
                string query = "SELECT ImageData, ImageType FROM images WHERE ImageID = @ImageID";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ImageID", id);

                // הרצת השאילתה
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    //  שליפת התמונה כ־byte array
                    byte[] imageData = (byte[])reader["ImageData"];

                    //  בדיקה אם קיים סוג תמונה, אחרת ברירת מחדל ל־image/jpeg
                    string imageType = reader.IsDBNull(reader.GetOrdinal("ImageType")) ? "image/jpeg" : reader.GetString(reader.GetOrdinal("ImageType"));

                    return File(imageData, imageType); // מחזירים את התמונה עצמה
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
