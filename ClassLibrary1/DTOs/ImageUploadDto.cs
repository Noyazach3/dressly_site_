using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ClassLibrary1.DTOs
{
    // DTO – Data Transfer Object: 
    // מחלקה זו משמשת כ"אמצעי העברה" של נתונים בין החזית (Blazor) לבין השרת (API),
    // כאשר מבצעים פעולה של העלאת תמונה לפריט לבוש – כולל מזהה הפריט וקובץ התמונה עצמו.

    public class ImageUploadDto
    {
        public int ItemID { get; set; }
        public IFormFile ImageFile { get; set; }
    }

}
