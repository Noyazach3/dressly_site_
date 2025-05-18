namespace ClassLibrary1.DTOs
{
    // DTO – Data Transfer Object: 
    // מחלקה זו משמשת כ"אמצעי העברה" של נתוני התחברות בין החזית (Blazor) לבין השרת (API),
    // כאשר המשתמש מנסה להתחבר למערכת עם אימייל וסיסמה.

    public class LoginRequest
    {

        public string Email { get; set; }
        public string Password { get; set; }
    }
}
