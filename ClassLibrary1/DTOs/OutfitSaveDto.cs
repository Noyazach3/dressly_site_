namespace ClassLibrary1.DTOs
{
    // DTO – Data Transfer Object: 
    // מחלקה זו משמשת כ"אמצעי העברה" של נתונים בין החזית (Blazor) לבין השרת (API),
    // כאשר יוצרים אאוטפיט חדש – היא מעבירה את מזהה המשתמש, שם האאוטפיט, תאריך היצירה,
    // ורשימת מזהי הפריטים שמרכיבים את האאוטפיט.

    public class OutfitSaveDto
    {
        public int? UserID { get; set; }
        public string Name { get; set; }
        public DateTime? DateCreated { get; set; }
        public List<int> ClothingItemIDs { get; set; } = new();
    }
}
