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
    public class AddressController : ControllerBase
    {
        private readonly FlowershopContext _context;
        private readonly ILogger<AddressController> _logger;

        public AddressController(FlowershopContext context, ILogger<AddressController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // API GET: Lấy danh sách địa chỉ của người dùng
        [HttpGet]
        public async Task<IActionResult> GetAddresses()
        {
            try
            {
                // Lấy user_id từ JWT token
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    _logger.LogError("User ID claim is missing or invalid in the JWT token.");
                    return Unauthorized("Không thể lấy thông tin người dùng từ token.");
                }

                // Lấy danh sách địa chỉ của người dùng và chỉ trả về các trường cần thiết
                var addresses = await _context.Addresses
                    .Where(a => a.UserInfo.UserId == userId)
                    .Select(a => new
                    {
                        a.AddressId,
                        a.UserInfoId,
                        a.Description
                    })
                    .ToListAsync();

                return Ok(addresses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting addresses for user ID {UserId}.", User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value);
                return StatusCode(500, "Có lỗi xảy ra khi lấy danh sách địa chỉ.");
            }
        }

        // API POST: Tạo mới địa chỉ
        [HttpPost]
        public async Task<IActionResult> CreateAddress([FromForm] CreateAddressDto addressDto)
        {
            try
            {
                // Lấy user_id từ JWT token
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    _logger.LogError("User ID claim is missing or invalid in the JWT token.");
                    return Unauthorized("Không thể lấy thông tin người dùng từ token.");
                }

                // Kiểm tra xem người dùng có tồn tại không
                var userInfo = await _context.UserInfos.FirstOrDefaultAsync(u => u.UserId == userId);
                if (userInfo == null)
                {
                    _logger.LogError("UserInfo for user ID {UserId} does not exist.", userId);
                    return NotFound("Thông tin người dùng không tồn tại.");
                }

                // Tạo địa chỉ mới
                var newAddress = new Address
                {
                    UserInfoId = userInfo.UserInfoId,
                    Description = addressDto.Description
                };

                _context.Addresses.Add(newAddress);
                await _context.SaveChangesAsync();

                return Ok(newAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating an address for user ID {UserId}.", User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value);
                return StatusCode(500, "Có lỗi xảy ra khi tạo địa chỉ.");
            }
        }

        // API DELETE: Xóa địa chỉ
        [HttpDelete("{addressId}")]
        public async Task<IActionResult> DeleteAddress(int addressId)
        {
            try
            {
                // Lấy user_id từ JWT token
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    _logger.LogError("User ID claim is missing or invalid in the JWT token.");
                    return Unauthorized("Không thể lấy thông tin người dùng từ token.");
                }

                // Lấy địa chỉ cần xóa
                var address = await _context.Addresses.FirstOrDefaultAsync(a => a.AddressId == addressId && a.UserInfo.UserId == userId);
                if (address == null)
                {
                    return NotFound("Không tìm thấy địa chỉ cần xóa.");
                }

                _context.Addresses.Remove(address);
                await _context.SaveChangesAsync();

                return Ok("Địa chỉ đã được xóa thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting an address for user ID {UserId}.", User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value);
                return StatusCode(500, "Có lỗi xảy ra khi xóa địa chỉ.");
            }
        }
    }
}
