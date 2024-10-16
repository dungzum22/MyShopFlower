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
                .ThenInclude(f => f.Seller) // Include thêm thông tin người bán
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var cartId = cartItems.FirstOrDefault()?.CartId;

            if (cartItems == null || !cartItems.Any())
            {
                return BadRequest("Giỏ hàng của bạn trống!");
            }

            // Lấy địa chỉ giao hàng từ request
            var deliveryAddress = await _context.Addresses
                .FirstOrDefaultAsync(a => a.AddressId == request.AddressId && a.UserInfoId == userId);
            if (deliveryAddress == null)
            {
                return BadRequest("Địa chỉ không hợp lệ.");
            }

            // Tạo đơn hàng tổng
            var order = new Order
            {
                UserId = userId,
                PaymentMethod = "VNPay",
                PhoneNumber = request.PhoneNumber,
                CreatedDate = DateTime.Now,
                AddressId = request.AddressId,
                DeliveryMethod = "Giao Hang Nhanh",
                CartId = cartId
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Nhóm các sản phẩm trong giỏ hàng theo người bán và tạo OrderDetail cho từng nhóm (shop)
            var groupedCartItems = cartItems.GroupBy(c => c.Flower.SellerId);
            decimal totalShippingFee = 0;
            decimal totalProductPrice = 0;

            foreach (var group in groupedCartItems)
            {
                // Lấy thông tin shop
                var seller = group.First().Flower.Seller;
                if (seller == null)
                {
                    return BadRequest("Không tìm thấy thông tin người bán cho sản phẩm này.");
                }

                // Lấy phí giao hàng cho toàn bộ sản phẩm của shop đó
                var shippingFeeResponse = await _ghnService.GetShippingFeeAsync(deliveryAddress.Description, seller.AddressSeller);
                if (!shippingFeeResponse.TryGetValue("total_fee", out var feeValue) || feeValue == null)
                {
                    return BadRequest("Không thể tính phí giao hàng.");
                }

                decimal shippingFee = Convert.ToDecimal(feeValue);
                totalShippingFee += shippingFee;

                bool isFirstItemInGroup = true;

                // Tạo chi tiết đơn hàng cho từng sản phẩm trong nhóm
                foreach (var item in group)
                {
                    var orderDetail = new OrdersDetail
                    {
                        OrderId = order.OrderId,
                        SellerId = item.Flower.SellerId,
                        FlowerId = item.FlowerId,
                        Price = item.Flower.Price * item.Quantity + (isFirstItemInGroup ? shippingFee : 0), // Thêm phí giao hàng vào sản phẩm đầu tiên của nhóm
                        Amount = item.Quantity,
                        Status = "pending",
                        CreatedAt = DateTime.Now,
                        AddressId = request.AddressId,
                        DeliveryMethod = "Giao Hang Nhanh"
                    };

                    totalProductPrice += item.Flower.Price * item.Quantity;
                    isFirstItemInGroup = false;

                    _context.OrdersDetails.Add(orderDetail);
                }
            }

            // Tính tổng giá trị đơn hàng bao gồm cả phí giao hàng
            order.TotalPrice = totalProductPrice + totalShippingFee;

            await _context.SaveChangesAsync();

            return Ok(new { OrderId = order.OrderId, TotalPrice = order.TotalPrice });

        }
    }
}