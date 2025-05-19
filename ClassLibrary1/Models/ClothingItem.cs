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

        public int? ImageID { get; set; }

        [Required(ErrorMessage = "שדה חובה")]
        public string Category { get; set; } // קטגורית הפריט

        public int? ColorID { get; set; } // מזהה הצבע – לא בשימוש כרגע

        [Required(ErrorMessage = "שדה חובה")]
        public string Season { get; set; }

        public DateTime? DateAdded { get; set; }


        [Required(ErrorMessage = "שדה חובה")]
        public string UsageType { get; set; } // סוג השימוש

        [Required(ErrorMessage = "שדה חובה")]
        public string ColorName { get; set; } // שם הצבע

        // קשרים לטבלאות אחרות
        public User User { get; set; }

        public Color Color { get; set; }
        public Image Image { get; set; } 

        public ICollection<OutfitItem> OutfitItems { get; set; }

        public ICollection<Favorite> Favorites { get; set; }

        public ICollection<ClothingItemTag> ClothingItemTags { get; set; }
    }
}
