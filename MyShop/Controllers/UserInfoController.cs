using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyShop.DataContext;
using MyShop.DTO;
using MyShop.Entities;
using System.Security.Claims;
using System.Threading.Tasks;
using Amazon.S3;
using MyShop.Services;

namespace MyShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]  // Chỉ cho phép người dùng đã xác thực qua JWT
    public class UserInfoController : ControllerBase
    {
        private readonly FlowershopContext _context;
        private readonly ILogger<UserInfoController> _logger;
        private readonly S3StorageService _s3StorageService;

        public UserInfoController(FlowershopContext context, ILogger<UserInfoController> logger, S3StorageService s3StorageService)
        {
            _context = context;
            _logger = logger;
            _s3StorageService = s3StorageService;
        }

        // API GET UserInfo: Lấy thông tin người dùng từ JWT
        [HttpGet("info")]
        public async Task<IActionResult> GetUserInfo()
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

                // Try to parse the user ID
                if (!int.TryParse(userIdClaim.Value, out int userId))
                {
                    _logger.LogError("Invalid user ID format in the token: {UserId}", userIdClaim.Value);
                    return Unauthorized("Invalid user ID format in the token.");
                }


                // Tìm UserInfo dựa trên user_id
                var userInfo = await _context.UserInfos.FirstOrDefaultAsync(u => u.UserId == userId);
                if (userInfo == null)
                {
                    _logger.LogWarning("UserInfo not found for UserId: {UserId}", userId);
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
            catch (Exception ex)
            {
                // Log any unexpected errors
                _logger.LogError(ex, "An error occurred while fetching user information.");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }



        // API PUT Update UserInfo: Cập nhật thông tin người dùng và upload hình ảnh
        [HttpPut("update")]
        public async Task<IActionResult> UpdateUserInfo([FromForm] UpdateUserInfoDto userInfoDto)
        {
            // Lấy user_id từ JWT token
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
            {
                return Unauthorized("Không thể lấy thông tin người dùng từ token.");
            }

            //int userId = int.Parse(userIdClaim.Value);
            var getUserId = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            int userId = int.Parse(getUserId.Value);
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
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(userInfoDto.Avatar.FileName)}";

                try
                {
                    using (var stream = userInfoDto.Avatar.OpenReadStream())
                    {
                        // Upload file lên S3 và lấy URL
                        var imageUrl = await _s3StorageService.UploadFileAsync(stream, fileName);
                        // Cập nhật đường dẫn ảnh vào cơ sở dữ liệu
                        userInfo.Avatar = imageUrl;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while uploading the avatar to S3.");
                    return StatusCode(500, "Có lỗi xảy ra khi tải ảnh lên.");
                }
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

        // API POST Create UserInfo: Tạo mới thông tin người dùng
        [HttpPost("create")]
        public async Task<IActionResult> CreateUserInfo([FromForm] CreateUserInfoDto createUserInfoDto)
        {
            // Lấy user_id từ JWT token
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
            {
                return Unauthorized("Không thể lấy thông tin người dùng từ token.");
            }

            //int userId = int.Parse(userIdClaim.Value);
            var getUserId = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            int userId = int.Parse(getUserId.Value);

            // Kiểm tra xem người dùng đã có UserInfo chưa
            var existingUserInfo = await _context.UserInfos.FirstOrDefaultAsync(u => u.UserId == userId);
            if (existingUserInfo != null)
            {
                return BadRequest("UserInfo đã tồn tại cho người dùng này.");
            }

            // Tạo mới UserInfo
            var newUserInfo = new UserInfo
            {
                UserId = userId,
                FullName = createUserInfoDto.FullName,
                Address = createUserInfoDto.Address,
                BirthDate = createUserInfoDto.BirthDate,
                Sex = createUserInfoDto.Sex,
                Points = 0,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            // Xử lý avatar nếu có file tải lên
            if (createUserInfoDto.Avatar != null && createUserInfoDto.Avatar.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(createUserInfoDto.Avatar.FileName)}";

                try
                {
                    using (var stream = createUserInfoDto.Avatar.OpenReadStream())
                    {
                        // Upload file lên S3 và lấy URL
                        var imageUrl = await _s3StorageService.UploadFileAsync(stream, fileName);
                        // Cập nhật đường dẫn ảnh vào cơ sở dữ liệu
                        newUserInfo.Avatar = imageUrl;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while uploading the avatar to S3.");
                    return StatusCode(500,$"Có lỗi xảy ra khi tải ảnh lên:{ex.Message}");
                }
            }

            // Thêm UserInfo mới vào cơ sở dữ liệu
            _context.UserInfos.Add(newUserInfo);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating user information.");
                return StatusCode(500, "An unexpected error occurred.");
            }

            return Ok("UserInfo đã được tạo thành công.");
        }
    }
}