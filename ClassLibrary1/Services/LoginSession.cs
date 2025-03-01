using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ClassLibrary1.Services
{
    public class LoginSession : ILoginSession
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoginSession(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;

            if (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false)
            {
                Console.WriteLine("🔹 המשתמש מחובר ✅");
                foreach (var claim in _httpContextAccessor.HttpContext.User.Claims)
                {
                    Console.WriteLine($"🔹 Claim: {claim.Type} = {claim.Value}");
                }
            }
            else
            {
                Console.WriteLine("❌ המשתמש **לא מחובר** או שאין לו `Claims`.");
            }
        }

        public string Role
        {
            get
            {
                var role = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value ?? "User";
                Console.WriteLine("🔹 תפקיד המשתמש: " + role);
                return role;
            }
        }

        public string UserId => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

}
