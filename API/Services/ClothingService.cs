using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClassLibrary1.Models;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace API.Services
{
    public class ClothingService
    {
        private readonly string _connectionString;

        public ClothingService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<int> GetTotalItemsAsync(int userId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT COUNT(*) FROM clothingitems WHERE UserID = @UserId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        public async Task<List<ClothingItem>> GetItemsForLaundryAsync(int userId)
        {
            var items = new List<ClothingItem>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT * FROM clothingitems WHERE UserID = @UserId AND TimesWorn >= WashAfterUses";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = new ClothingItem
                            {
                                ItemID = reader.GetInt32(reader.GetOrdinal("ItemID")),
                                UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                Category = reader.GetString(reader.GetOrdinal("Category")),
                                ColorID = reader.GetInt32(reader.GetOrdinal("ColorID")),
                                Season = reader.GetString(reader.GetOrdinal("Season")),
                                ImageID = reader.IsDBNull(reader.GetOrdinal("ImageID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("ImageID")),
                                DateAdded = reader.IsDBNull(reader.GetOrdinal("DateAdded")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DateAdded")),
                                LastWornDate = reader.IsDBNull(reader.GetOrdinal("LastWornDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("LastWornDate")),
                                UsageType = reader.GetString(reader.GetOrdinal("UsageType")),
                                ColorName = reader.GetString(reader.GetOrdinal("ColorName"))
                            };
                            items.Add(item);
                        }
                    }
                }
            }
            return items;
        }

        public async Task AddClothingItemAsync(ClothingItem item)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"
                INSERT INTO clothingitems (UserID, Category, ColorID, Season, ImageData, DateAdded, WashAfterUses, LastWornDate)
                VALUES (@UserID, @Category, @ColorID, @Season, @ImageData, @DateAdded, @WashAfterUses, NULL)";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", item.UserID);
                    command.Parameters.AddWithValue("@Category", item.Category);
                    command.Parameters.AddWithValue("@ColorID", item.ColorID);
                    command.Parameters.AddWithValue("@Season", item.Season);
                    command.Parameters.AddWithValue("@DateAdded", item.DateAdded ?? DateTime.Now);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<bool> DeleteClothingItemAsync(int itemId, int userId)
        {
            if (!await IsAdminAsync(userId))
            {
                return false;
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "DELETE FROM clothingitems WHERE ItemID = @ItemID";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ItemID", itemId);
                    await command.ExecuteNonQueryAsync();
                }
            }

            return true;
        }

        private async Task<bool> IsAdminAsync(int userId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT Role FROM users WHERE UserID = @UserId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    var role = await command.ExecuteScalarAsync();
                    return role != null && role.ToString() == "Admin";
                }
            }
        }

        public async Task<object> GetClothingStatisticsAsync()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string clothingCountQuery = "SELECT COUNT(*) FROM clothingitems";
                int totalClothingItems;
                using (var cmd = new MySqlCommand(clothingCountQuery, connection))
                {
                    totalClothingItems = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                string popularCategoriesQuery = @"
                SELECT Category, COUNT(*) as Count 
                FROM clothingitems 
                GROUP BY Category 
                ORDER BY Count DESC
                LIMIT 5";

                var popularCategories = new List<object>();
                using (var cmd = new MySqlCommand(popularCategoriesQuery, connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            popularCategories.Add(new
                            {
                                Category = reader.GetString(reader.GetOrdinal("Category")),
                                Count = Convert.ToInt32(reader["Count"])
                            });
                        }
                    }
                }

                return new
                {
                    TotalClothingItems = totalClothingItems,
                    PopularCategories = popularCategories
                };
            }
        }
    }
}
