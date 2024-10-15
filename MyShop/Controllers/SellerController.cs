using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyShop.DataContext;
using MyShop.DTO;
using MyShop.Entities;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyShop.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]  // Người dùng cần đăng nhập để truy cập
    public class SellerController : ControllerBase
    {
        private readonly FlowershopContext _context;
        private readonly ILogger<SellerController> _logger;

        public SellerController(FlowershopContext context, ILogger<SellerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // API POST: Đăng ký làm seller
        [HttpPost("register")]
        public async Task<IActionResult> RegisterSeller([FromForm] RegisterSellerDto sellerDto)
        {
            try
            {
                // Lấy user_id từ JWT token
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");

                if (userIdClaim == null)
                {
                    _logger.LogError("User ID claim is missing from the JWT token.");
                    return Unauthorized("Không thể lấy thông tin người dùng từ token.");
                }

                if (!int.TryParse(userIdClaim.Value, out int userId))
                {
                    _logger.LogError("Invalid user ID format in the token: {UserId}", userIdClaim.Value);
                    return Unauthorized("Invalid user ID format in the token.");
                }

                // Kiểm tra xem người dùng đã đăng ký làm seller chưa
                var existingSeller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
                if (existingSeller != null)
                {
                    return BadRequest("Người dùng đã đăng ký làm seller trước đó.");
                }

                // Tạo mới Seller
                var newSeller = new Seller
                {
                    UserId = userId,
                    ShopName = sellerDto.ShopName,
                    Introduction = sellerDto.Introduction,
                    AddressSeller = sellerDto.AddressSeller,
                    Role = sellerDto.Role,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Thêm Seller mới vào cơ sở dữ liệu
                _context.Sellers.Add(newSeller);

                // Cập nhật thông tin UserInfo để đánh dấu là seller
                var userInfo = await _context.UserInfos.FirstOrDefaultAsync(u => u.UserId == userId);
                if (userInfo != null)
                {
                    userInfo.IsSeller = true;
                    _context.UserInfos.Update(userInfo);
                }
                // Cập nhật thông tin trong bảng Users để thay đổi type thành 'seller'
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user != null)
                {
                    user.Type = "seller";
                    _context.Users.Update(user);
                }



                await _context.SaveChangesAsync();

                return Ok("Đăng ký làm seller thành công.");
            }
            catch (Exception ex)
            {
                // Log lỗi nếu có lỗi bất ngờ xảy ra
                _logger.LogError(ex, "An error occurred while registering as a seller.");
                return StatusCode(500, $"Có lỗi xảy ra khi đăng ký làm seller. {ex}");
            }
        }
    }


}