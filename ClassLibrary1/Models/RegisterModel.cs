namespace ClassLibrary1.Models
{
    public class RegisterModel
    {
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty; // אצלך במקום FirstName + LastName
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "User"; // ברירת מחדל – משתמש רגיל
    }
}
