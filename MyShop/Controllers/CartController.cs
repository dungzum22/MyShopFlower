using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyShop.DataContext;
using MyShop.DTO;
using MyShop.Entities;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace MyShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Chỉ cho phép người dùng đã xác thực qua JWT
    public class CartController : ControllerBase
    {
        private readonly FlowershopContext _context;
        private readonly ILogger<CartController> _logger;
        private readonly S3StorageService _s3StorageService;

        public CartController(FlowershopContext context, ILogger<CartController> logger, S3StorageService s3StorageService)
        {
            _context = context;
            _logger = logger;
            _s3StorageService = s3StorageService;
        }

        // API GET: Lấy thông tin giỏ hàng
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            // Lấy user_id từ JWT token
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
            {
                return Unauthorized("Không thể lấy thông tin người dùng từ token.");
            }

            int userId;
            if (!int.TryParse(userIdClaim.Value, out userId))
            {
                return Unauthorized("UserId không hợp lệ.");
            }

            // Tìm giỏ hàng dựa trên user_id
            var cartItems = await _context.Carts
                .Include(c => c.Flower)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (cartItems == null || !cartItems.Any())
            {
                return NotFound("Giỏ hàng của bạn đang trống.");
            }

            // Lấy URL ảnh từ AWS S3 bằng S3StorageService
            var cartItemDtos = new List<CartItemDto>();
            foreach (var cartItem in cartItems)
            {
                // Sử dụng trực tiếp đường dẫn từ bảng Flower_Info
                string imageUrl = cartItem.Flower?.ImageUrl;

                // Nếu cần, bạn có thể thêm kiểm tra để đảm bảo URL không null hoặc rỗng
                if (string.IsNullOrEmpty(imageUrl))
                {
                    imageUrl = "path/to/default/image.jpg"; // Đặt một URL mặc định nếu không có ảnh
                }

                // Tạo đối tượng DTO cho từng sản phẩm trong giỏ hàng
                cartItemDtos.Add(new CartItemDto
                {
                    FlowerName = cartItem.Flower?.FlowerName,
                    ImageUrl = imageUrl,
                    Price = cartItem.Flower?.Price ?? 0,
                    Quantity = cartItem.Quantity
                });
            }

            // Tính tổng giá tiền và tổng số lượng sản phẩm trong giỏ hàng
            var cartSummary = new CartSummaryDto
            {
                Items = cartItemDtos,
                TotalQuantity = cartItems.Sum(c => c.Quantity),
                TotalPrice = cartItems.Sum(c => c.Quantity * (c.Flower?.Price ?? 0))
            };

            // Trả về danh sách sản phẩm trong giỏ hàng và tổng giá trị
            return Ok(cartSummary);
        }

        // API POST: Thêm sản phẩm vào giỏ hàng
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromQuery] int flowerId, [FromQuery] int quantity)
        {
            // Lấy user_id từ JWT token
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
            {
                return Unauthorized("Không thể lấy thông tin người dùng từ token.");
            }

            int userId;
            if (!int.TryParse(userIdClaim.Value, out userId))
            {
                return Unauthorized("UserId không hợp lệ.");
            }

            // Kiểm tra xem sản phẩm có tồn tại không
            var flower = await _context.FlowerInfos.FindAsync(flowerId);
            if (flower == null)
            {
                return NotFound("Sản phẩm này không tồn tại.");
            }

            // Kiểm tra số lượng sản phẩm có sẵn
            if (quantity > flower.AvailableQuantity)
            {
                return BadRequest("Số lượng yêu cầu vượt quá số lượng có sẵn của sản phẩm.");
            }

            // Kiểm tra xem sản phẩm đã có trong giỏ chưa, nếu có thì cập nhật số lượng
            var cartItem = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId && c.FlowerId == flowerId);
            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                cartItem = new Cart
                {
                    UserId = userId,
                    FlowerId = flowerId,
                    Quantity = quantity
                };
                _context.Carts.Add(cartItem);
            }

            // Cập nhật số lượng sản phẩm có sẵn
            flower.AvailableQuantity -= quantity;

            await _context.SaveChangesAsync();
            return Ok("Sản phẩm đã được thêm vào giỏ hàng.");
        }

        // API DELETE: Xóa sản phẩm khỏi giỏ hàng
        [HttpDelete("remove/{flowerId}")]
        public async Task<IActionResult> RemoveFromCart(int flowerId)
        {
            // Lấy user_id từ JWT token
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
            {
                return Unauthorized("Không thể lấy thông tin người dùng từ token.");
            }

            int userId;
            if (!int.TryParse(userIdClaim.Value, out userId))
            {
                return Unauthorized("UserId không hợp lệ.");
            }

            var cartItem = await _context.Carts.Include(c => c.Flower).FirstOrDefaultAsync(c => c.FlowerId == flowerId && c.UserId == userId);
            if (cartItem == null)
            {
                return NotFound("Sản phẩm này không tồn tại trong giỏ hàng hoặc không phải của bạn.");
            }

            // Cập nhật số lượng sản phẩm có sẵn
            if (cartItem.Flower != null)
            {
                cartItem.Flower.AvailableQuantity += cartItem.Quantity;
            }

            _context.Carts.Remove(cartItem);
            await _context.SaveChangesAsync();
            return Ok("Sản phẩm đã được xóa khỏi giỏ hàng.");
        }
    }
}