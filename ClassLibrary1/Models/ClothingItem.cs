using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace ClassLibrary1.Models
{
    public class ClothingItem
    {
        [Key] // מגדיר את ItemID כמפתח ראשי
        public int ItemID { get; set; }
        public int UserID { get; set; } // מזהה המשתמש
        public string Category { get; set; }
        public int ColorID { get; set; } // מזהה הצבע
        public string Season { get; set; }
        public string ImageURL { get; set; }
        public DateTime? DateAdded { get; set; }
        public DateTime? LastWornDate { get; set; }
        public int WashAfterUses { get; set; } = 1;
        public string UsageType { get; set; } // סוג השימוש
        public string ColorName { get; set; } // שם הצבע

        public User User { get; set; }
        public Color Color { get; set; }
        public ICollection<OutfitItem> OutfitItems { get; set; }    
        public ICollection<Favorite> Favorites { get; set; }
        public ICollection<ClothingItemTag> ClothingItemTags { get; set; }
        public ICollection<ClothingItemUsage> ClothingItemUsages { get; set; }
    }
}
