using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleLogin.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginGoogle : ControllerBase
    {
        // GET: api/logingoogle/login
        [HttpGet("login")]
        public IActionResult Login()
        {
            // Bắt đầu quá trình đăng nhập với Google
            var properties = new AuthenticationProperties { RedirectUri = "/api/logingoogle/callback" };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // GET: api/logingoogle/callback
        [HttpGet("callback")]
        public async Task<IActionResult> Callback()
        {
            // Nhận thông tin người dùng sau khi đăng nhập thành công
            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded)
            {
                return Unauthorized();
            }

            // Xử lý thông tin người dùng (ví dụ: lấy email hoặc tên người dùng)
            var claims = authenticateResult.Principal.Identities
                .FirstOrDefault()?.Claims
                .Select(claim => new
                {
                    claim.Type,
                    claim.Value
                });

            return Ok(claims);
        }

        // GET: api/logingoogle/profile (yêu cầu người dùng đã đăng nhập)
        [Authorize]
        [HttpGet("profile")]
        public IActionResult Profile()
        {
            // Lấy thông tin người dùng đã đăng nhập
            var userName = User.Identity?.Name;
            return Ok(new { Message = $"Hello, {userName}" });
        }
    }
}

