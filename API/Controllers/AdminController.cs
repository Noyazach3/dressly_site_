using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Services;
using ClassLibrary1.Services;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ILoginSession _loginSession;

        public AdminController(IAdminService adminService, ILoginSession loginSession)
        {
            _adminService = adminService;
            _loginSession = loginSession;
        }

        private string GetUserRole()
        {
            return _loginSession.Role;
        }

        [HttpGet("GetUsers")]
        public async Task<IActionResult> GetUsers()
        {
            if (GetUserRole() != "Admin")
            {
                return Unauthorized();
            }

            var users = await _adminService.GetUsersAsync();
            return Ok(users);
        }

        [HttpDelete("DeleteUser")]
        public async Task<IActionResult> DeleteUser([FromQuery] string username)
        {
            if (GetUserRole() != "Admin")
            {
                return Unauthorized();
            }

            var result = await _adminService.DeleteUserAsync(username);
            return result ? Ok("המשתמש נמחק בהצלחה") : NotFound("המשתמש לא נמצא");
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromQuery] string username)
        {
            if (GetUserRole() != "Admin")
            {
                return Unauthorized();
            }

            var result = await _adminService.ResetPasswordAsync(username);
            return result ? Ok("סיסמת המשתמש אופסה בהצלחה") : NotFound("המשתמש לא נמצא");
        }
    }
}
