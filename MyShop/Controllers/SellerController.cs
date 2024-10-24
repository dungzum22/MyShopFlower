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


        [HttpGet("GetAllSellers")]

        public async Task<IActionResult> GetAllSellers()
        {
            try
            {
                // Lấy tất cả seller từ cơ sở dữ liệu
                var sellers = await _context.Sellers.ToListAsync();

                // Nếu không có seller nào trong cơ sở dữ liệu
                if (sellers == null || !sellers.Any())
                {
                    return NotFound(new { message = "Không tìm thấy người bán nào." });
                }

                // Trả về danh sách seller với các trường cần thiết
                return Ok(sellers.Select(s => new
                {
                    SellerId = s.SellerId,
                    UserId = s.UserId,
                    ShopName = s.ShopName,
                    AddressSeller = s.AddressSeller,
                    TotalProduct = s.TotalProduct,
                    Quantity = s.Quantity,
                    Role = s.Role,
                    Introduction = s.Introduction,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                }));
            }
            catch (Exception ex)
            {
                // Log lỗi nếu có sự cố
                _logger.LogError(ex, "Có lỗi xảy ra khi lấy danh sách người bán.");
                return StatusCode(500, "Có lỗi xảy ra khi lấy danh sách người bán.");
            }
        }
        [HttpPut("UpdateSeller")]
        [Authorize(Roles = "seller")] // Chỉ seller mới có quyền cập nhật thông tin
        public async Task<IActionResult> UpdateSeller([FromForm] UpdateSellerDto sellerDto)
        {
            try
            {
                // Lấy userId từ token JWT
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    _logger.LogError("User ID claim is missing or invalid in the JWT token.");
                    return Unauthorized("Bạn phải đăng nhập để cập nhật thông tin người bán.");
                }

                // Tìm seller theo UserId (mỗi user chỉ có một seller)
                var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
                if (seller == null)
                {
                    return NotFound("Người bán không tồn tại.");
                }

                // Cập nhật các thông tin của seller từ DTO
                seller.ShopName = sellerDto.ShopName ?? seller.ShopName;  // Cập nhật nếu có giá trị mới
                seller.AddressSeller = sellerDto.AddressSeller ?? seller.AddressSeller;
                seller.Introduction = sellerDto.Introduction ?? seller.Introduction;
                seller.Role = sellerDto.Role ?? seller.Role; // Cập nhật vai trò nếu cần
                seller.UpdatedAt = DateTime.UtcNow;  // Cập nhật thời gian chỉnh sửa cuối cùng

                // Lưu lại thay đổi
                _context.Sellers.Update(seller);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Cập nhật thông tin người bán thành công.",
                    seller = new
                    {
                        SellerId = seller.SellerId,
                        UserId = seller.UserId,
                        ShopName = seller.ShopName,
                        AddressSeller = seller.AddressSeller,
                        Introduction = seller.Introduction,
                        Role = seller.Role,
                        UpdatedAt = seller.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Có lỗi xảy ra khi cập nhật thông tin người bán.");
                return StatusCode(500, "Có lỗi xảy ra khi cập nhật thông tin người bán.");
            }
        }

        [HttpGet("GetCurrentSeller")]
        [Authorize(Roles = "seller")] // Chỉ người dùng có vai trò seller mới có thể truy cập
        public async Task<IActionResult> GetCurrentSeller()
        {
            try
            {
                // Lấy UserId từ JWT token
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    _logger.LogError("User ID claim is missing or invalid in the JWT token.");
                    return Unauthorized("Bạn phải đăng nhập để lấy thông tin người bán.");
                }

                // Tìm seller theo UserId
                var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
                if (seller == null)
                {
                    return NotFound("Người bán không tồn tại.");
                }

                // Trả về thông tin của seller
                return Ok(new
                {
                    SellerId = seller.SellerId,
                    UserId = seller.UserId,
                    ShopName = seller.ShopName,
                    AddressSeller = seller.AddressSeller,
                    Role = seller.Role,
                    Introduction = seller.Introduction,
                    TotalProduct = seller.TotalProduct,
                    Quantity = seller.Quantity,
                    CreatedAt = seller.CreatedAt,
                    UpdatedAt = seller.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Có lỗi xảy ra khi lấy thông tin người bán.");
                return StatusCode(500, "Có lỗi xảy ra khi lấy thông tin người bán.");
            }
        }


    }


}