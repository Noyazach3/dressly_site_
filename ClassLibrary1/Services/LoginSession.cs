namespace ClassLibrary1.Services
{
    public class LoginSession
    {
        // ערכי ברירת מחדל עבור אורח
        public int UserID { get; private set; } = 0;  // התאמה לשיטה של המורה
        public string Username { get; private set; } = "Guest";
        public string Email { get; private set; } = "guest@site.com";
        public string Role { get; private set; } = "Guest";

        // פונקציה לקביעת פרטי המשתמש המחובר
        public void SetLoginDetails(int userId, string username, string email, string role)
        {
            this.UserID = userId;
            this.Username = username;
            this.Email = email;
            this.Role = role;
        }

        // פונקציה לניקוי ההתחברות
        public void ClearSession()
        {
            UserID = 0;
            Username = "Guest";
            Email = "guest@site.com";
            Role = "Guest"; // תמיד מאפס ל-Guest
        }

        // פונקציה לבדוק אם המשתמש הוא אורח
        public bool IsGuest()
        {
            return Role == "Guest";
        }
    }
}
