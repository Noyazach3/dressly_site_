namespace ClassLibrary1.Models
{
    // מחלקת Model:
    // מייצגת את המידע הבסיסי של משתמש כפי שמוצג בממשק הניהול – כולל מזהה, שם משתמש, אימייל ותפקיד.
    // משמשת להצגת נתוני משתמשים בצד ה־Blazor לצורכי צפייה, חיפוש ומחיקה (ללא סיסמה או פרטי התחברות).

    public class UserInfoModel
    {
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
