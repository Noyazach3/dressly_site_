using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace ClassLibrary1.Services
{
    //  מחלקה זו משמשת לשמירת מצב ההתחברות של המשתמש במהלך השימוש באתר (Session).
    // היא משותפת בין כל הרכיבים (Singleton), ומאפשרת לגשת לנתוני המשתמש מכל מקום בפרויקט.
    public class LoginSession 
    {
        // ערכי ברירת מחדל עבור אורח
        public int UserID { get;  set; } = 0;
        public string Username { get;  set; } = "Guest";
        public string Email { get;  set; } = "guest@site.com";
        public string Role { get;  set; } = "Guest";


        //  פעולה לשמירת פרטי המשתמש לאחר התחברות מוצלחת.
        public void SetLoginDetails(int userId, string username, string email, string role)
        {
            UserID = userId;
            Username = username;
            Email = email;
            Role = role;
        }


        //  איפוס כל פרטי המשתמש – משמש בעת התנתקות מהמערכת.
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