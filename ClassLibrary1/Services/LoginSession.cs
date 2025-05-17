using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace ClassLibrary1.Services
{
    public class LoginSession 
    {
        // ערכי ברירת מחדל עבור אורח
        public int UserID { get;  set; } = 0;
        public string Username { get;  set; } = "Guest";
        public string Email { get;  set; } = "guest@site.com";
        public string Role { get;  set; } = "Guest";

        public void SetLoginDetails(int userId, string username, string email, string role)
        {
            UserID = userId;
            Username = username;
            Email = email;
            Role = role;
        }

        public void ClearSession()
        {
            UserID = 0;
            Username = "Guest";
            Email = "guest@site.com";
            Role = "Guest";
        }

        // פונקציה לבדוק אם המשתמש הוא אורח
        public bool IsGuest()
        {
            return Role == "Guest";
        }
    }
}