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

                // Nếu user là Seller, lấy thêm thông tin từ bảng Seller
                SellerDto sellerInfo = null;
                if (userInfo.IsSeller == true)
                {
                    var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
                    if (seller != null)
                    {
                        sellerInfo = new SellerDto
                        {
                            SellerId = seller.SellerId,
                            ShopName = seller.ShopName,
                            AddressSeller = seller.AddressSeller,
                            Introduction = seller.Introduction,
                            Role = seller.Role,
                            TotalProduct = seller.TotalProduct,
                            Quantity = seller.Quantity,
                            CreatedAt = seller.CreatedAt ?? DateTime.UtcNow,
                            UpdatedAt = seller.UpdatedAt ?? DateTime.MinValue
                        };
                    }
                }

                // Trả về thông tin UserInfo và SellerInfo nếu có
                if (userInfo.IsSeller == true)
                {
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
                        userInfo.UpdatedDate,
                        Role = "Seller",
                        SellerInfo = sellerInfo
                    });
                }
                else
                {
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
                        userInfo.UpdatedDate,
                        Role = "User bình thường"
                    });
                }
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
        public async Task<IActionResult> UpdateUserInfo([FromBody] UpdateUserInfoDto userInfoDto)
        {
            // Lấy user_id từ JWT token
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
            {
                _logger.LogError("User ID claim is missing from the JWT token.");
                return Unauthorized("Không thể lấy thông tin người dùng từ token.");
            }

            // Chuyển đổi userId từ chuỗi sang số nguyên
            if (!int.TryParse(userIdClaim.Value, out int userId))
            {
                _logger.LogError("Invalid user ID format in the token: {UserId}", userIdClaim.Value);
                return Unauthorized("Định dạng user ID không hợp lệ.");
            }

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
                var filePath = $"user-info/{fileName}"; // Thêm prefix user-info vào tên file

                try
                {
                    using (var stream = userInfoDto.Avatar.OpenReadStream())
                    {
                        // Upload file lên S3 và lấy URL
                        var imageUrl = await _s3StorageService.UploadFileAsync(stream, filePath);
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

            // Nếu là người bán, lấy thêm thông tin từ bảng Seller
            SellerDto sellerInfo = null;
            if (userInfo.IsSeller == true)
            {
                var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
                if (seller != null)
                {
                    sellerInfo = new SellerDto
                    {
                        SellerId = seller.SellerId,
                        ShopName = seller.ShopName,
                        AddressSeller = seller.AddressSeller,
                        Introduction = seller.Introduction,
                        Role = seller.Role,
                        TotalProduct = seller.TotalProduct,
                        Quantity = seller.Quantity,
                        CreatedAt = seller.CreatedAt ?? DateTime.UtcNow,
                        UpdatedAt = seller.UpdatedAt ?? DateTime.MinValue
                    };
                }
            }

            // Trả về thông tin UserInfo và SellerInfo nếu có
            if (userInfo.IsSeller == true)
            {
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
                    userInfo.UpdatedDate,
                    Role = "Seller",
                    SellerInfo = sellerInfo
                });
            }
            else
            {
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
                    userInfo.UpdatedDate,
                    Role = "User bình thường"
                });
            }
        }


        // API POST Create UserInfo: Tạo mới thông tin người dùng
        [HttpPost("create")]
        public async Task<IActionResult> CreateUserInfo([FromBody] CreateUserInfoDto createUserInfoDto)
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
                Points = 100,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            // Xử lý avatar nếu có file tải lên
            if (createUserInfoDto.Avatar != null && createUserInfoDto.Avatar.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(createUserInfoDto.Avatar.FileName)}";
                var filePath = $"user-info/{fileName}"; // Thêm prefix user-info vào tên file

                try
                {
                    using (var stream = createUserInfoDto.Avatar.OpenReadStream())
                    {
                        // Upload file lên S3 và lấy URL
                        var imageUrl = await _s3StorageService.UploadFileAsync(stream, filePath);
                        // Cập nhật đường dẫn ảnh vào cơ sở dữ liệu
                        newUserInfo.Avatar = imageUrl;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while uploading the avatar to S3.");
                    return StatusCode(500, $"Có lỗi xảy ra khi tải ảnh lên:{ex.Message}");
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

        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                // Truy vấn tất cả UserInfo từ cơ sở dữ liệu
                var allUsers = await _context.UserInfos
                    .Select(userInfo => new
                    {
                        userInfo.UserInfoId,
                        userInfo.FullName,
                        userInfo.Address,
                        userInfo.BirthDate,
                        userInfo.Sex,
                        userInfo.Avatar,
                        userInfo.Points,
                        userInfo.CreatedDate,
                        userInfo.UpdatedDate,
                        IsSeller = userInfo.IsSeller
                    })
                    .ToListAsync();

                // Kiểm tra nếu không có người dùng nào
                if (allUsers == null || !allUsers.Any())
                {
                    return NotFound("Không có thông tin người dùng nào.");
                }

                // Thêm thông tin seller nếu người dùng là seller
                var detailedUsers = new List<object>();

                foreach (var user in allUsers)
                {
                    if (user.IsSeller == true)
                    {
                        var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.UserInfoId);
                        var sellerInfo = new
                        {
                            SellerId = seller?.SellerId ?? 0,
                            ShopName = seller?.ShopName,
                            AddressSeller = seller?.AddressSeller,
                            Introduction = seller?.Introduction,
                            Role = seller?.Role,
                            TotalProduct = seller?.TotalProduct ?? 0,
                            Quantity = seller?.Quantity ?? 0,
                            CreatedAt = seller?.CreatedAt ?? DateTime.UtcNow,
                            UpdatedAt = seller?.UpdatedAt ?? DateTime.MinValue
                        };

                        detailedUsers.Add(new
                        {
                            user.UserInfoId,
                            user.FullName,
                            user.Address,
                            user.BirthDate,
                            user.Sex,
                            user.Avatar,
                            user.Points,
                            user.CreatedDate,
                            user.UpdatedDate,
                            Role = "Seller",
                            SellerInfo = sellerInfo
                        });
                    }
                    else
                    {
                        detailedUsers.Add(new
                        {
                            user.UserInfoId,
                            user.FullName,
                            user.Address,
                            user.BirthDate,
                            user.Sex,
                            user.Avatar,
                            user.Points,
                            user.CreatedDate,
                            user.UpdatedDate,
                            Role = "User bình thường"
                        });
                    }
                }

                // Trả về danh sách chi tiết người dùng
                return Ok(detailedUsers);
            }
            catch (Exception ex)
            {
                // Ghi log nếu có lỗi xảy ra
                _logger.LogError(ex, "An error occurred while fetching all users information.");
                return StatusCode(500, "Có lỗi không mong muốn xảy ra.");
            }
        }

    }
}