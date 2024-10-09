using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyShop.Services.ApplicationDbContext;
using MyShop.Entities;
using MyShop.Services.ApplicationDbContext;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleLogin.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginGoogle : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LoginGoogle(ApplicationDbContext context)
        {
            _context = context;
        }

        // Bắt đầu quá trình đăng nhập với Google
        [HttpGet("signin-goole")]
        public IActionResult Login()
        {
            var properties = new AuthenticationProperties { RedirectUri = "/api/logingoogle/callback" };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // Xử lý callback sau khi đăng nhập thành công
        [HttpGet("callback")]
        public async Task<IActionResult> Callback()
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded)
            {
                return Unauthorized();
            }

            // Lấy email từ thông tin xác thực
            var emailClaim = authenticateResult.Principal.FindFirst(claim => claim.Type == System.Security.Claims.ClaimTypes.Email);
            var email = emailClaim?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email không được tìm thấy.");
            }

            // Kiểm tra xem người dùng đã tồn tại trong cơ sở dữ liệu chưa
            var existingUser = _context.Users.FirstOrDefault(u => u.Email == email);
            if (existingUser == null)
            {
                // Thêm người dùng mới vào database
                var newUser = new User
                {
                    Username = authenticateResult.Principal.Identity.Name ?? email,
                    Email = email,
                    CreatedDate = DateTime.UtcNow,
                    Type = "Google",
                    Status = "Active"
                };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Nếu người dùng đã tồn tại, cập nhật thông tin (nếu cần)
                existingUser.Status = "Active";
                await _context.SaveChangesAsync();
            }

            return Ok(new { Message = $"Đăng nhập thành công, email: {email}" });

        }

        // Endpoint yêu cầu người dùng đã đăng nhập
        [Authorize]
        [HttpGet("profile")]
        public IActionResult Profile()
        {
            var userName = User.Identity?.Name;
            return Ok(new { Message = $"Xin chào, {userName}" });
        }
    }
}
