using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyShop.DataContext;
using MyShop.DTO;
using MyShop.Entities;
using MyShop.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyShop.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]  // Người dùng cần đăng nhập để truy cập
    public class OrderController : ControllerBase
    {
        private readonly FlowershopContext _context;
        private readonly ILogger<OrderController> _logger;
        private readonly IGHNService _ghnService;

        public OrderController(FlowershopContext context, ILogger<OrderController> logger, IGHNService ghnService)
        {
            _context = context;
            _logger = logger;
            _ghnService = ghnService;
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
            var cartItems = await _context.Carts
                .Include(c => c.Flower)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (cartItems == null || !cartItems.Any())
            {
                return BadRequest("Giỏ hàng của bạn trống!");
            }

            // Lấy địa chỉ giao hàng được chọn từ request
            var deliveryAddress = await _context.Addresses.FirstOrDefaultAsync(a => a.AddressId == request.AddressId && a.UserInfoId == userId);
            if (deliveryAddress == null)
            {
                return BadRequest("Địa chỉ không hợp lệ.");
            }

            // Tạo đơn hàng tổng
            var order = new Order
            {
                UserId = userId,
                PhoneNumber = request.PhoneNumber,
                //PaymentMethod = request.PaymentMethod,
                CreatedDate = DateTime.Now,
                AddressId = request.AddressId,

            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Nhóm các sản phẩm trong giỏ hàng theo người bán và tạo OrderDetail cho từng nhóm
            var groupedCartItems = cartItems.GroupBy(c => c.Flower.SellerId);
            decimal totalShippingFee = 0;

            foreach (var group in groupedCartItems)
            {
                foreach (var item in group)
                {
                    // Lấy phí giao hàng từ MockGHNService
                    var shippingFeeResponse = await _ghnService.GetShippingFeeAsync(deliveryAddress.Description, item.Flower.Seller.AddressSeller);
                    if (!shippingFeeResponse.TryGetValue("total_fee", out var feeValue) || feeValue == null)
                    {
                        return BadRequest("Không thể tính phí giao hàng.");
                    }

                    decimal shippingFee = Convert.ToDecimal(feeValue);
                    totalShippingFee += shippingFee;

                    var orderDetail = new OrdersDetail
                    {
                        OrderId = order.OrderId,
                        SellerId = item.Flower.SellerId,
                        FlowerId = item.FlowerId,
                        Price = item.Flower.Price,
                        Amount = item.Quantity,
                        Status = "pending",
                        CreatedAt = DateTime.Now,
                        AddressId = request.AddressId,
                        DeliveryMethod = "Giao Hang Nhanh"
                    };

                    _context.OrdersDetails.Add(orderDetail);
                }
            }

            // Tính tổng giá trị đơn hàng bao gồm cả phí giao hàng
            order.TotalPrice = cartItems.Sum(c => c.Quantity * c.Flower.Price) + totalShippingFee;

            await _context.SaveChangesAsync();

            return Ok(new { OrderId = order.OrderId, TotalPrice = order.TotalPrice });
        }
    }
}
