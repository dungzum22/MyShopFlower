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
        private readonly IVNPayService _vnpayService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(FlowershopContext context, IGHNService ghnService, IVNPayService vnpayService, ILogger<OrderController> logger)
        {
            _context = context;
            _ghnService = ghnService;
            _vnpayService = vnpayService;
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

            // Nhóm các sản phẩm trong giỏ hàng theo người bán
            var groupedCartItems = cart.GroupBy(c => c.Flower.SellerId).ToList();

            // Duyệt qua từng nhóm sản phẩm và tạo đơn hàng cho mỗi người bán
            var createdOrders = new List<Order>();

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
                var order = new Order
                {
                    UserId = userId,
                    PhoneNumber = request.PhoneNumber,
                    DeliveryMethod = "Giao Hang Nhanh",
                    Status = "pending",
                    CreatedDate = DateTime.Now,
                    AddressId = userAddress.AddressId,
                    PaymentMethod = "VNPay",
                    SellerId = sellerId
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



                // Call VNPay để xử lý thanh toán
                try
                {
                    var transactionId = await _vnpayService.ProcessPaymentAsync(totalPrice, request.PhoneNumber);
                    order.TransactionId = transactionId;
                }
                catch (HttpRequestException)
                {
                    return BadRequest("Thanh toán thất bại!");
                }

                // Lưu đơn hàng vào cơ sở dữ liệu
                _context.Orders.Add(order);
                createdOrders.Add(order);
            }

            await _context.SaveChangesAsync();

            return Ok(createdOrders);
        }
    }
}
