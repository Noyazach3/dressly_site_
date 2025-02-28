namespace ClassLibrary1.Services
{
    public interface ILoginSession
    {
        string Role { get; }
        string UserId { get; }
    }
}
