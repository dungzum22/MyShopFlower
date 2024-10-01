using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyShop.DataContext;
using MyShop.DTO;
using MyShop.Entities;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]  // Chỉ cho phép người dùng đã xác thực qua JWT
    public class UserInfoController : ControllerBase
    {
        private readonly FlowershopContext _context;

        public UserInfoController(FlowershopContext context)
        {
            _context = context;
        }

        // API GET UserInfo: Lấy thông tin người dùng từ JWT
        [HttpGet("info")]
        public async Task<IActionResult> GetUserInfo()
        {
            // Lấy user_id từ JWT token
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
            if (userIdClaim == null)
            {
                return Unauthorized("Không thể lấy thông tin người dùng từ token.");
            }

            int userId = int.Parse(userIdClaim.Value);

            // Tìm UserInfo dựa trên user_id
            var userInfo = await _context.UserInfos.FirstOrDefaultAsync(u => u.UserId == userId);
            if (userInfo == null)
            {
                return NotFound("UserInfo không tồn tại cho người dùng này.");
            }

            // Trả về thông tin UserInfo
            return Ok(new
            {
                userInfo.UserInfoId,
                userInfo.FullName,
                userInfo.Address,
                userInfo.BirthDate,
                userInfo.Sex,
                userInfo.Avatar,
                userInfo.Points,
                userInfo.CreatedDate,
                userInfo.UpdatedDate
            });
        }

        // API PUT Update UserInfo: Cập nhật thông tin người dùng và upload hình ảnh
        [HttpPut("update")]
        public async Task<IActionResult> UpdateUserInfo([FromForm] UpdateUserInfoDto userInfoDto)
        {
            // Lấy user_id từ JWT token (ví dụ)
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("Không thể lấy thông tin người dùng từ token.");
            }

            int userId = int.Parse(userIdClaim.Value);

            // Tìm UserInfo dựa trên user_id
            var userInfo = await _context.UserInfos.FirstOrDefaultAsync(u => u.UserId == userId);
            if (userInfo == null)
            {
                return NotFound("UserInfo không tồn tại cho người dùng này.");
            }

            // Cập nhật các thông tin từ DTO vào model UserInfo
            userInfo.FullName = userInfoDto.FullName;
            userInfo.Address = userInfoDto.Address;
            userInfo.BirthDate = userInfoDto.BirthDate;
            userInfo.Sex = userInfoDto.Sex;

            // Xử lý avatar nếu có file tải lên
            if (userInfoDto.Avatar != null && userInfoDto.Avatar.Length > 0)
            {
                var fileName = Path.GetFileName(userInfoDto.Avatar.FileName);
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/avatars");

                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var filePath = Path.Combine(uploadsPath, fileName);

                // Lưu file ảnh vào thư mục
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await userInfoDto.Avatar.CopyToAsync(stream);
                }

                // Cập nhật đường dẫn ảnh vào cơ sở dữ liệu
                userInfo.Avatar = $"/uploads/avatars/{fileName}";
            }

            // Cập nhật ngày chỉnh sửa
            userInfo.UpdatedDate = DateTime.UtcNow;

            // Đánh dấu entity là đã thay đổi và lưu thay đổi vào cơ sở dữ liệu
            _context.Entry(userInfo).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.UserInfos.Any(e => e.UserInfoId == userInfo.UserInfoId))
                {
                    return NotFound("Không tìm thấy UserInfo này.");
                }
                else
                {
                    throw;
                }
            }

            return Ok("UserInfo đã được cập nhật thành công.");
        }

    }
}
