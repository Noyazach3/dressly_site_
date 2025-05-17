using System;
using System.ComponentModel.DataAnnotations;

namespace ClassLibrary1.DTOs
{
    /// <summary>
    /// מחלקה זו מייצגת את מבנה הנתונים שנשלח מהטופס להוספת פריט לבוש.
    /// היא כוללת רק את השדות שהמשתמש ממלא בפועל, בלי קשרים לטבלאות אחרות.
    /// </summary>
    public class ClothingItemDto
    {
        public int ItemID { get; set; }

        public int UserID { get; set; }

        [Required(ErrorMessage = "שדה חובה")]
        public string Category { get; set; }

        [Required(ErrorMessage = "שדה חובה")]
        public string Season { get; set; }

        [Required(ErrorMessage = "שדה חובה")]
        public string UsageType { get; set; }

        [Required(ErrorMessage = "שדה חובה")]
        public string ColorName { get; set; }
      
        public int? ImageID { get; set; }

        public DateTime? DateAdded { get; set; }
    }
}
