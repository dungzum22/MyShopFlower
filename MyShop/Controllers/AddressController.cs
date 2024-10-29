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
    [Authorize]
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
                var userInfo = await _context.UserInfos
                    .Include(u => u.Addresses)
                    .FirstOrDefaultAsync(u => u.UserId == userId);
                if (userInfo == null)
                {
                    _logger.LogError("UserInfo for user ID {UserId} does not exist.", userId);
                    return NotFound("Thông tin người dùng không tồn tại.");
                }

                // Kiểm tra xem địa chỉ đã tồn tại chưa
                bool isAddressDuplicate = userInfo.Addresses
                    .Any(a => a.Description.Trim().Equals(addressDto.Description.Trim(), StringComparison.OrdinalIgnoreCase));

                if (isAddressDuplicate)
                {
                    return BadRequest("Địa chỉ đã tồn tại. Vui lòng nhập địa chỉ khác.");
                }

                // Tạo địa chỉ mới
                var newAddress = new Address
                {
                    UserInfoId = userInfo.UserInfoId,
                    Description = addressDto.Description
                };

                _context.Addresses.Add(newAddress);
                await _context.SaveChangesAsync();

                // Chuyển đổi `userInfo` sang `UserInfoDto` để trả về
                var userInfoDto = new UserInfoDto
                {


                    Addresses = userInfo.Addresses.Select(a => new AddressDto
                    {
                        AddressId = a.AddressId,
                        UserInfoId = userInfo.UserInfoId,
                        Description = a.Description
                    }).ToList()
                };

                return Ok(userInfoDto); // Trả về toàn bộ thông tin người dùng cùng danh sách địa chỉ
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating an address for user ID {UserId}.", User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value);
                return StatusCode(500, "Có lỗi xảy ra khi tạo địa chỉ.");
            }
        }



        [HttpDelete("{addressId}")]
        public async Task<IActionResult> DeleteAddress(int addressId)
        {
            try
            {

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    _logger.LogError("User ID claim is missing or invalid in the JWT token.");
                    return Unauthorized("Không thể lấy thông tin người dùng từ token.");
                }


                var address = await _context.Addresses.FirstOrDefaultAsync(a => a.AddressId == addressId && a.UserInfo.UserId == userId);
                if (address == null)
                {
                    return NotFound("Không tìm thấy địa chỉ cần xóa.");
                }

                _context.Addresses.Remove(address);
                await _context.SaveChangesAsync();


                var remainingAddresses = await _context.Addresses
                    .Where(a => a.UserInfo.UserId == userId)
                    .Select(a => new AddressDto
                    {
                        AddressId = a.AddressId,
                        Description = a.Description
                    })
                    .ToListAsync();

                return Ok(remainingAddresses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting an address for user ID {UserId}.", User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value);
                return StatusCode(500, "Có lỗi xảy ra khi xóa địa chỉ.");
            }
        }

        [HttpPut("{addressId}")]
        public async Task<IActionResult> UpdateAddress(int addressId, [FromForm] UpdateAddressDto addressDto)
        {
            try
            {

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    _logger.LogError("User ID claim is missing or invalid in the JWT token.");
                    return Unauthorized("Không thể lấy thông tin người dùng từ token.");
                }


                var address = await _context.Addresses.FirstOrDefaultAsync(a => a.AddressId == addressId && a.UserInfo.UserId == userId);
                if (address == null)
                {
                    return NotFound("Không tìm thấy địa chỉ cần cập nhật hoặc bạn không có quyền cập nhật địa chỉ này.");
                }
                bool isDuplicate = await _context.Addresses
            .AnyAsync(a => a.UserInfo.UserId == userId
                           && a.AddressId != addressId
                           && a.Description.Trim().Equals(addressDto.Description.Trim(), StringComparison.OrdinalIgnoreCase));

                if (isDuplicate)
                {
                    return BadRequest("Địa chỉ đã tồn tại. Vui lòng nhập địa chỉ khác.");
                }

                if (!string.IsNullOrEmpty(addressDto.Description))
                {
                    address.Description = addressDto.Description;
                }

                _context.Entry(address).State = EntityState.Modified;
                await _context.SaveChangesAsync();


                var updatedAddresses = await _context.Addresses
                    .Where(a => a.UserInfo.UserId == userId)
                    .Select(a => new AddressDto
                    {
                        AddressId = a.AddressId,
                        Description = a.Description
                    })
                    .ToListAsync();

                return Ok(updatedAddresses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the address for user ID {UserId}.", User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value);
                return StatusCode(500, "Có lỗi xảy ra khi cập nhật địa chỉ.");
            }
        }



    }
}
