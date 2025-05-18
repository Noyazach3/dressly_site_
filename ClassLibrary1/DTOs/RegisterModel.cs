namespace ClassLibrary1.DTOs
{
    // DTO – Data Transfer Object: 
    // מחלקה זו משמשת כ"אמצעי העברה" של נתונים בין החזית (Blazor) לבין השרת (API),
    // כאשר משתמש נרשם למערכת – היא כוללת את שם המשתמש, אימייל, סיסמה (מוצפנת מראש),
    // ותפקיד המשתמש (ברירת מחדל: "User").

    public class RegisterModel
    {
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty; // אצלך במקום FirstName + LastName
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "User"; // ברירת מחדל – משתמש רגיל
    }
}
