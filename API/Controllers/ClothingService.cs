using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClassLibrary1.Models;
using Microsoft.Extensions.Configuration;
using System.Data;

public class ClothingService
{
    private readonly string _connectionString;

    public ClothingService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    // ------------------------------------------------------------------
    // פעולה לשליפת כמות כללית של בגדים לפי משתמש
    // SELECT COUNT(*) FROM clothingitems WHERE UserID = @UserId
    // ------------------------------------------------------------------
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

    // ------------------------------------------------------------------
    // שליפת בגדים שדורשים כביסה
    // SELECT * FROM clothingitems WHERE UserID = @UserId AND TimesWorn >= WashAfterUses
    // ------------------------------------------------------------------
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
                        items.Add(new ClothingItem
                        {
                            ItemID = reader.GetInt32("ItemID"),
                            UserID = reader.GetInt32("UserID"),
                            Category = reader.GetString("Category"),
                            ColorID = reader.GetInt32("ColorID"),
                            Season = reader.GetString("Season"),
                            ImageURL= reader.GetString("ImageURL"),
                            DateAdded = reader.IsDBNull("DateAdded") ? (DateTime?)null : reader.GetDateTime("DateAdded"),
                            LastWornDate = reader.IsDBNull("LastWornDate") ? (DateTime?)null : reader.GetDateTime("LastWornDate"),
                            WashAfterUses = reader.GetInt32("WashAfterUses"),
                            UsageType = reader.GetString("UsageType"),
                            ColorName = reader.GetString("ColorName")
                        });
                    }
                }
            }
        }

        return items;
    }

    // ------------------------------------------------------------------
    // הוספת פריט חדש לארון המשתמש
    // INSERT INTO clothingitems (UserID, Category, ColorID, Season, ImageURL, DateAdded, WashAfterUses, LastWornDate)
    // VALUES (@UserID, @Category, @ColorID, @Season, @ImageURL, @DateAdded, @WashAfterUses, NULL)
    // ------------------------------------------------------------------
    public async Task AddClothingItemAsync(ClothingItem item)
    {
        using (var connection = new MySqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var query = @"
            INSERT INTO clothingitems (UserID, Category, ColorID, Season, ImageURL, DateAdded, WashAfterUses, LastWornDate)
            VALUES (@UserID, @Category, @ColorID, @Season, @ImageURL, @DateAdded, @WashAfterUses, NULL)";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserID", item.UserID);
                command.Parameters.AddWithValue("@Category", item.Category);
                command.Parameters.AddWithValue("@ColorID", item.ColorID);
                command.Parameters.AddWithValue("@Season", item.Season);
                command.Parameters.AddWithValue("@ImageURL", item.ImageData);
                command.Parameters.AddWithValue("@DateAdded", item.DateAdded ?? DateTime.Now);
                command.Parameters.AddWithValue("@WashAfterUses", item.WashAfterUses);

                await command.ExecuteNonQueryAsync();
            }
        }
    }

    // ------------------------------------------------------------------
    // מחיקת פריט בגדים (Admin בלבד)
    // DELETE FROM clothingitems WHERE ItemID = @ItemID
    // ------------------------------------------------------------------
    public async Task<bool> DeleteClothingItemAsync(int itemId, int userId)
    {
        if (!await IsAdminAsync(userId))
        {
            return false; // רק מנהל יכול למחוק פריטים
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

    // ------------------------------------------------------------------
    // בדיקה אם המשתמש הוא מנהל
    // SELECT Role FROM users WHERE UserID = @UserId
    // ------------------------------------------------------------------
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

    // ------------------------------------------------------------------
    // שליפת סטטיסטיקות בגדים (כמות בגדים, קטגוריות פופולריות)
    // ------------------------------------------------------------------
    public async Task<object> GetClothingStatisticsAsync()
    {
        using (var connection = new MySqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            // כמות כללית של בגדים
            string clothingCountQuery = "SELECT COUNT(*) FROM clothingitems";
            int totalClothingItems;
            using (var cmd = new MySqlCommand(clothingCountQuery, connection))
            {
                totalClothingItems = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // קטגוריות בגדים פופולריות
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
                            Category = reader["Category"].ToString(),
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
