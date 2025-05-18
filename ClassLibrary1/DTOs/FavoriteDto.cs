namespace ClassLibrary1.DTOs
{
    // DTO – Data Transfer Object: 
    // מחלקה זו משמשת כ"אמצעי העברה" של נתונים בין החזית (Blazor) לבין השרת (API),
    // כאשר מבצעים פעולות שקשורות למועדפים – כמו הוספה או הסרה של פריט לבוש או אאוטפיט מהרשימה האישית של המשתמש.

    public class FavoriteDto
    {
        public int UserID { get; set; }
        public int OutfitID { get; set; }
        public int? ItemID { get; set; }


    }
}