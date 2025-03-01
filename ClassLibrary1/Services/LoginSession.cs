using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ClassLibrary1.Services
{
    public class LoginSession
    {
        // ערכי ברירת מחדל עבור אורח
        public string Username { get; private set; } = "Guest";
        public string Email { get; private set; } = "guest@site.com";
        public string Role { get; private set; } = "Guest";

        public void SetLoginDetails( string Username, string email, string role)
        {
            Username = Username;
            Email = email;
            Role = role;
        }

        public void ClearSession()
        {
            if (Role == "Admin")
                Role = "Admin";
            else
                Role = "Guest";

            // חזרה לערכי ברירת מחדל של אורח
            Username = "Guest";
            Email = "guest@site.com";
            
        }

        // פונקציה לבדוק אם המשתמש הוא אורח
        
    }

}
