using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClassLibrary1.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string Role { get; set; } = "User"; // כל משתמש חדש מוגדר כ-User כברירת מחדל


        public ICollection<ClothingItem> ClothingItems { get; set; }
        public ICollection<Outfit> Outfits { get; set; }
        public ICollection<Favorite> Favorites { get; set; }
    }
}
