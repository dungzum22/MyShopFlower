using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using MyShop.DTO;
using MyShop.Entities;
using MyShop.Services;
using MyShop.DataContext;

namespace MyShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly FlowershopContext _context;
        private readonly IGHNService _ghnService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(FlowershopContext context, IGHNService ghnService, ILogger<OrderController> logger)
        {
            _context = context;
            _ghnService = ghnService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequestDto request)
        {
            // Lấy userId từ token JWT
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
            {
                return Unauthorized("Không xác định được người dùng.");
            }
            if (!int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("UserId không hợp lệ.");
            }

            // Lấy thông tin giỏ hàng của người dùng
            var cart = await _context.Carts
                .Include(c => c.Flower)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (cart == null || !cart.Any())
            {
                return BadRequest("Giỏ hàng của bạn trống!");
            }

            // Tạo đơn hàng tổng
            var order = new Order
            {
                UserId = userId,
                PhoneNumber = request.PhoneNumber,
                Status = "pending",
                CreatedDate = DateTime.Now,
                AddressId = (await _context.Addresses.FirstOrDefaultAsync(a => a.UserInfoId == userId))?.AddressId,
                PaymentMethod = "VNPay"
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Nhóm các sản phẩm trong giỏ hàng theo người bán và tạo OrderDetail cho từng nhóm
            var groupedCartItems = cart.GroupBy(c => c.Flower.SellerId).ToList();
            var createdOrderDetails = new List<OrdersDetail>();

            foreach (var group in groupedCartItems)
            {
                int sellerId = group.Key ?? 0;

                // Lấy địa chỉ của người bán từ bảng Sellers
                var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.SellerId == sellerId && sellerId != 0);
                if (seller == null)
                {
                    return BadRequest($"Không tìm thấy thông tin của người bán với ID: {sellerId}.");
                }

                string sellerAddress = seller.AddressSeller;

                // Lấy địa chỉ của người dùng
                var userAddress = await _context.Addresses.FirstOrDefaultAsync(a => a.UserInfoId == userId);
                if (userAddress == null)
                {
                    return BadRequest("Địa chỉ của bạn không tồn tại!");
                }

                // Tính tổng giá cho nhóm sản phẩm của người bán hiện tại
                decimal totalPrice = group.Sum(item => item.Quantity * item.Flower.Price);

                // Tạo chi tiết đơn hàng cho người bán
                var orderDetail = new OrdersDetail
                {
                    OrderId = order.OrderId,
                    SellerId = sellerId,
                    TotalPrice = totalPrice,
                    DeliveryMethod = "Giao Hang Nhanh",
                    Status = "pending",
                    CartId = group.First().CartId
                };

                // Tính phí giao hàng giả lập
                try
                {
                    var shippingFee = await _ghnService.GetShippingFeeAsync(userAddress.Description, sellerAddress);

                    // Đảm bảo cast đúng kiểu dữ liệu
                    if (shippingFee.ContainsKey("total_fee"))
                    {
                        _logger.LogInformation("Phí giao hàng: {ShippingFee}", shippingFee["total_fee"].ToString());
                    }
                    else
                    {
                        return BadRequest("Phản hồi không hợp lệ từ GHN Service.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Tính phí giao hàng thất bại: {Message}", ex.Message);
                    return BadRequest($"Không thể tính phí giao hàng. Chi tiết lỗi: {ex.Message}");
                }

                // Lưu chi tiết đơn hàng vào cơ sở dữ liệu
                _context.OrdersDetails.Add(orderDetail);
                createdOrderDetails.Add(orderDetail);
            }

            await _context.SaveChangesAsync();

            return Ok(new { order, createdOrderDetails });
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            // Lấy userId từ token JWT
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
            {
                return Unauthorized("Không xác định được người dùng.");
            }
            if (!int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("UserId không hợp lệ.");
            }

            // Lấy danh sách đơn hàng của người dùng
            var orders = await _context.Orders
                .Include(o => o.Address)
                .Include(o => o.OrdersDetails) // Bao gồm thông tin chi tiết đơn hàng
                .ThenInclude(od => od.Cart) // Bao gồm thông tin giỏ hàng
                .Where(o => o.UserId == userId)
                .ToListAsync();

            if (orders == null || !orders.Any())
            {
                return NotFound("Không tìm thấy đơn hàng nào.");
            }

            return Ok(orders);
        }
    }
}