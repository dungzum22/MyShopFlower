using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MyShop.Entities;
using MyShop.Services.Users;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using MimeKit;
using MailKit.Net.Smtp;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

public class AuthController : Controller
{
    private readonly IUserService _userService;
    private readonly IConfiguration _config;

    public AuthController(IUserService userService, IConfiguration config)
    {
        _userService = userService;
        _config = config;
    }

    [HttpPost("login")]
    public IActionResult Login([FromForm] string username, [FromForm] string password)
    {
        // Kiểm tra thông tin đăng nhập
        var user = _userService.Authenticate(username, password);

        if (user == null)
            return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không chính xác." });

        if (user.Status == "inactive")
        {
            return StatusCode(403, new { message = "Tài khoản của bạn đã bị ban." }); // Trả về lỗi 403 với thông báo
        }

        // Tạo JWT token
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]);

        // Định nghĩa các claims cho token


        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
        new Claim("UserId", user.UserId.ToString()), // Lưu UserId từ bảng User
        new Claim(ClaimTypes.Name, user.Username),   // Lưu Username từ bảng User
        new Claim(ClaimTypes.Role, user.Type)        // Lưu vai trò (role) của người dùng
            }),
            Expires = DateTime.UtcNow.AddHours(1), // Token có hiệu lực trong 1 giờ
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        // Trả về thông tin đăng nhập kèm token và type (role)
        return Ok(new
        {
            UserId = user.UserId,
            Username = user.Username,
            Type = user.Type,  // Thêm type vào thông tin trả về
            Token = tokenString,
            Expiration = tokenDescriptor.Expires
        });
    }

    [HttpPost("register")]
    public IActionResult Register([FromForm] string username, [FromForm] string password, [FromForm] string email)
    {
        // Kiểm tra xem username đã tồn tại chưa
        if (_userService.CheckUsernameExists(username))
        {
            return BadRequest(new { message = "Tên đăng nhập đã tồn tại." });
        }

        // Kiểm tra xem email đã tồn tại chưa
        if (_userService.CheckEmailExists(email))
        {
            return BadRequest(new { message = "Email đã tồn tại." });
        }

        // Tạo user mới với mật khẩu đã mã hóa
        var newUser = new User
        {
            Username = username,
            Password = BCrypt.Net.BCrypt.HashPassword(password), // Mã hóa mật khẩu
            Email = email,
            CreatedDate = DateTime.UtcNow,
            Type = "user",  // Loại người dùng mặc định
            Status = "active"
        };

        var createdUser = _userService.Register(newUser);

        if (createdUser == null)
        {
            return BadRequest(new { message = "Đăng ký không thành công." });
        }

        // Gửi email xác nhận đăng ký thành công
        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress("MyShop", _config["EmailSettings:SenderEmail"]));
        emailMessage.To.Add(new MailboxAddress(newUser.Username, newUser.Email));
        emailMessage.Subject = "Chào mừng bạn đã đăng ký thành công tại MyShop";
        emailMessage.Body = new TextPart("plain")
        {
            Text = $"Xin chào {newUser.Username},\n\nCảm ơn bạn đã đăng ký tài khoản tại MyShop.\n\nChúng tôi hy vọng bạn sẽ có những trải nghiệm tuyệt vời khi sử dụng dịch vụ của chúng tôi.\n\nTrân trọng,\nĐội ngũ MyShop"
        };

        using (var client = new SmtpClient())
        {
            // Sử dụng StartTls cho cổng 587 hoặc SslOnConnect cho cổng 465
            client.Connect(_config["EmailSettings:SmtpServer"], int.Parse(_config["EmailSettings:Port"]), MailKit.Security.SecureSocketOptions.StartTls);
            client.Authenticate(_config["EmailSettings:SenderEmail"], _config["EmailSettings:SenderPassword"]);
            client.Send(emailMessage);
            client.Disconnect(true);
        }

        // Trả về thông tin người dùng đã được tạo
        return Ok(new
        {
            UserId = createdUser.UserId,
            Username = createdUser.Username,
            Email = createdUser.Email,
            Status = createdUser.Status
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        _userService.Logout();
        return Ok(new { message = "Đã đăng xuất thành công" });
    }

    [HttpPut("update-status/{userId}")]
    public IActionResult UpdateUserStatus(int userId)
    {
        var user = _userService.GetUserById(userId);
        if (user == null)
        {
            return NotFound(new { message = "Không tìm thấy người dùng." });
        }

        // Cập nhật trạng thái của người dùng
        user.Status = user.Status == "active" ? "inactive" : "active";  // Đổi từ active thành inactive và ngược lại
        _userService.UpdateUser(user);

        return Ok(new { message = "Trạng thái của người dùng đã được cập nhật.", newStatus = user.Status });
    }
    [HttpGet("all-users")]
    public IActionResult GetAllUsers()
    {
        var users = _userService.GetAllUsers()
            .Select(user => new
            {
                user.UserId,
                user.Username,
                user.Email,
                user.Type,
                user.CreatedDate,
                user.Status
            }).ToList();

        return Ok(users);
    }


}