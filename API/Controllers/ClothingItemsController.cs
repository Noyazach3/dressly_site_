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




        [HttpPost("addClothingItem")]
        public async Task<IActionResult> AddClothingItem([FromForm] ClothingItemDto itemDto, IFormFile imageFile)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                itemDto.DateAdded = DateTime.Now;

                int newItemId;
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string insertQuery = @"
                INSERT INTO clothingitems (UserID, Category, Season, DateAdded, UsageType, ColorName)
                VALUES (@UserID, @Category, @Season, @DateAdded, @UsageType, @ColorName);
                SELECT LAST_INSERT_ID();";

                    using (var command = new MySqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@UserID", itemDto.UserID);
                        command.Parameters.AddWithValue("@Category", itemDto.Category);
                        command.Parameters.AddWithValue("@Season", itemDto.Season);
                        command.Parameters.AddWithValue("@DateAdded", itemDto.DateAdded);
                        command.Parameters.AddWithValue("@UsageType", itemDto.UsageType);
                        command.Parameters.AddWithValue("@ColorName", itemDto.ColorName);

                        newItemId = Convert.ToInt32(await command.ExecuteScalarAsync());
                    }
                }

                if (imageFile != null && imageFile.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await imageFile.CopyToAsync(memoryStream);

                    var image = new Image
                    {
                        OwnerID = newItemId,
                        ImageData = memoryStream.ToArray(),
                        ImageType = imageFile.ContentType,
                        UploadDate = DateTime.Now
                    };

                    var imageId = await UploadImage(image);

                    using var connection = new MySqlConnection(connectionString);
                    await connection.OpenAsync();
                    var updateQuery = "UPDATE clothingitems SET ImageID = @ImageID WHERE ItemID = @ItemID";

                    using var command = new MySqlCommand(updateQuery, connection);
                    command.Parameters.AddWithValue("@ImageID", imageId);
                    command.Parameters.AddWithValue("@ItemID", newItemId);
                    await command.ExecuteNonQueryAsync();
                }

                return Ok(newItemId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error adding clothing item", Error = ex.Message });
            }
        }

        [HttpPost("uploadImageForItem")]
        public async Task<IActionResult> UploadImageForItem([FromForm] ImageUploadDto dto)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                var image = new Image
                {
                    OwnerID = dto.ItemID,
                    ImageData = await ConvertToByteArray(dto.ImageFile),
                    UploadDate = DateTime.Now,
                    ImageType = dto.ImageFile.ContentType
                };

                int imageId = await UploadImage(image);

                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string updateQuery = "UPDATE clothingitems SET ImageID = @ImageID WHERE ItemID = @ItemID";
                    using (var command = new MySqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@ImageID", imageId);
                        command.Parameters.AddWithValue("@ItemID", dto.ItemID);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                return Ok("Image uploaded successfully.");
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


        