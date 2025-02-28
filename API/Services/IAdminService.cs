using System.Collections.Generic;
using System.Threading.Tasks;
using ClassLibrary1.Models;

namespace API.Services
{
    public interface IAdminService
    {
        // שליפת כל המשתמשים במערכת
        Task<List<User>> GetUsersAsync();

        // מחיקת משתמש לפי שם משתמש
        Task<bool> DeleteUserAsync(string username);

        // איפוס סיסמה של משתמש
        Task<bool> ResetPasswordAsync(string username);
    }
}
