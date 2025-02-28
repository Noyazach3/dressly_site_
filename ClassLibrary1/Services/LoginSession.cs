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
        }

        public string Role => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        public string UserId => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }
}
