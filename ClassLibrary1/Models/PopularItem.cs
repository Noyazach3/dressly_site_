using System;
using System.Collections.Generic;
using ClassLibrary1.DTOs;

namespace ClassLibrary1.Models
{
    public class PopularItem: ClothingItemDto // מחלקת ירושה
    {
        public int ItemCount { get; set; } // כמה פעמים הפריט נוצר
    }
}