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
    public class VoucherController : ControllerBase
    {
        private readonly FlowershopContext _context;
        private readonly ILogger<VoucherController> _logger;

        public VoucherController(FlowershopContext context, ILogger<VoucherController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // API GET: Lấy danh sách voucher của người dùng
        [HttpGet]
        public async Task<IActionResult> GetVouchers()
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

                // Lấy thông tin user_info dựa trên user_id
                var userInfo = await _context.UserInfos.FirstOrDefaultAsync(u => u.UserId == userId);
                if (userInfo == null)
                {
                    return NotFound("Không tìm thấy thông tin người dùng.");
                }

                // Lấy danh sách voucher của người dùng
                var vouchers = await _context.UserVoucherStatuses
                    .Where(v => v.UserInfoId == userInfo.UserInfoId)
                    .ToListAsync();

                if (!vouchers.Any())
                {
                    return Ok("Người dùng không có voucher nào.");
                }

                return Ok(vouchers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting vouchers for user ID {UserId}.", User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value);
                return StatusCode(500, "Có lỗi xảy ra khi lấy danh sách voucher.");
            }
        }

        // API POST: Tạo mới voucher
        [HttpPost]

        public async Task<IActionResult> CreateVoucher([FromForm] CreateVoucherDto voucherDto)
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

                // Lấy thông tin seller dựa trên user_id
                var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
                if (seller == null)
                {
                    return NotFound("Không tìm thấy thông tin người bán.");
                }

                // Lấy danh sách tất cả người dùng (loại bỏ seller và admin)
                var users = await _context.UserInfos
                    .Include(u => u.User)
                    .Where(u => u.User.Type == "user")
                    .ToListAsync();

                if (!users.Any())
                {
                    return NotFound("Không có người dùng nào để tạo voucher.");
                }

                // Tạo voucher cho tất cả người dùng
                foreach (var user in users)
                {
                    var newVoucher = new UserVoucherStatus
                    {
                        UserInfoId = user.UserInfoId,
                        ShopId = seller.SellerId,
                        VoucherCode = voucherDto.VoucherCode,
                        Discount = voucherDto.Discount,
                        Description = voucherDto.Description,
                        StartDate = voucherDto.StartDate,
                        EndDate = voucherDto.EndDate,
                        UsageLimit = voucherDto.UsageLimit,
                        UsageCount = 0,
                        RemainingCount = voucherDto.UsageLimit,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.UserVoucherStatuses.Add(newVoucher);
                }

                await _context.SaveChangesAsync();

                return Ok("Voucher đã được tạo thành công cho tất cả người dùng.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating vouchers for all users.");
                return StatusCode(500, "Có lỗi xảy ra khi tạo voucher.");
            }
        }


        // API DELETE: Xóa voucher
        [HttpDelete("{userVoucherStatusId}")]
        public async Task<IActionResult> DeleteVoucher(int userVoucherStatusId)
        {
            try
            {
                var voucher = await _context.UserVoucherStatuses.FirstOrDefaultAsync(v => v.UserVoucherStatusId == userVoucherStatusId);
                if (voucher == null)
                {
                    _logger.LogWarning("Voucher with ID {UserVoucherStatusId} not found.", userVoucherStatusId);
                    return NotFound(new { message = "Không tìm thấy voucher cần xóa." });
                }

                _context.UserVoucherStatuses.Remove(voucher);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Voucher đã được xóa thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting voucher with ID {UserVoucherStatusId}.", userVoucherStatusId);
                return StatusCode(500, new { message = "Có lỗi xảy ra khi xóa voucher." });
            }
        }
    }
}


