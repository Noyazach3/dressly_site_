namespace ClassLibrary1.Services
{
    public interface ILoginSession
    {
        int UserID { get; }
        string Username { get; }
        string Email { get; }
        string Role { get; }

        void SetLoginDetails(int userId, string username, string email, string role);
        void ClearSession();
        bool IsGuest();
    }
}
