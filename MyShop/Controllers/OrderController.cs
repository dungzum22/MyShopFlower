using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyShop.DataContext;
using MyShop.DTO;
using MyShop.Entities;
using MyShop.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Web;
using System.Security.Cryptography;
using System.Text;

namespace MyShop.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]  
    public class OrderController : ControllerBase
    {
        private readonly FlowershopContext _context;
        private readonly ILogger<OrderController> _logger;
        private readonly IGHNService _ghnService;
        private readonly IConfiguration _configuration;
        private readonly VNPayService _vnPayService;

        public OrderController(FlowershopContext context, ILogger<OrderController> logger, IGHNService ghnService, IConfiguration configuration, VNPayService vnPayService)
        {
            _context = context;
            _logger = logger;
            _ghnService = ghnService;
            _configuration = configuration; // Inject IConfiguration
            _vnPayService = vnPayService;
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
                .ThenInclude(f => f.Seller)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var cartId = cartItems.FirstOrDefault()?.CartId;

            if (cartItems == null || !cartItems.Any())
            {
                return BadRequest("Giỏ hàng của bạn trống!");
            }

            // Lấy địa chỉ giao hàng từ request
            var deliveryAddress = await _context.Addresses
                .FirstOrDefaultAsync(a => a.AddressId == request.AddressId);

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

                // Kiểm tra xem người dùng có cung cấp voucher cho shop này không
                decimal discountAmount = 0;
                int? appliedVoucherStatusId = null;
                if (request.VoucherIds != null && request.VoucherIds.Any())
                {
                    var voucherId = request.VoucherIds.FirstOrDefault(vid => _context.UserVoucherStatuses.Any(v => v.UserVoucherStatusId == vid && v.ShopId == seller.SellerId));
                    if (voucherId != 0)
                    {
                        var voucher = await _context.UserVoucherStatuses
                            .FirstOrDefaultAsync(v => v.UserVoucherStatusId == voucherId
                                                    && v.UserInfoId == userId
                                                    && v.ShopId == seller.SellerId
                                                    && v.RemainingCount > 0
                                                    && v.StartDate <= DateTime.Now
                                                    && v.EndDate >= DateTime.Now);

                        if (voucher != null)
                        {
                            var groupTotalPrice = group.Sum(item => item.Flower.Price * item.Quantity);
                            discountAmount = groupTotalPrice * (decimal)voucher.Discount / 100;
                            appliedVoucherStatusId = voucher.UserVoucherStatusId;

                            voucher.RemainingCount -= 1;
                            voucher.UsageCount += 1;

                            _context.UserVoucherStatuses.Update(voucher);
                        }
                    }
                }

                decimal groupTotalPriceBeforeDiscount = group.Sum(item => item.Flower.Price * item.Quantity);
                decimal discountAllocated = 0;

                foreach (var item in group)
                {
                    var itemPrice = item.Flower.Price * item.Quantity;
                    decimal itemDiscount = 0;

                    if (groupTotalPriceBeforeDiscount > 0 && discountAmount > 0)
                    {
                        itemDiscount = (itemPrice / groupTotalPriceBeforeDiscount) * discountAmount;
                        discountAllocated += itemDiscount;
                    }

                    var orderDetail = new OrdersDetail
                    {
                        OrderId = order.OrderId,
                        SellerId = item.Flower.SellerId,
                        FlowerId = item.FlowerId,
                        Price = (itemPrice - itemDiscount) + (isFirstItemInGroup ? shippingFee : 0),
                        Amount = item.Quantity,
                        Status = "pending",
                        CreatedAt = DateTime.Now,
                        AddressId = request.AddressId,
                        DeliveryMethod = "Giao Hang Nhanh",
                        UserVoucherStatusId = appliedVoucherStatusId
                    };

                    totalProductPrice += itemPrice - itemDiscount;
                    isFirstItemInGroup = false;

                    _context.OrdersDetails.Add(orderDetail);
                }

                if (discountAllocated != discountAmount)
                {
                    var lastOrderDetail = _context.OrdersDetails.LastOrDefault(od => od.OrderId == order.OrderId);
                    if (lastOrderDetail != null)
                    {
                        lastOrderDetail.Price -= (discountAllocated - discountAmount);
                    }
                }
            }

            decimal totalPriceValue = totalProductPrice + totalShippingFee;
            order.TotalPrice = totalPriceValue;  // Không làm tròn, sử dụng giá trị chính xác

            await _context.SaveChangesAsync();

            // Ghi log chi tiết giá trị totalPriceValue và ipAddress
            _logger.LogInformation("Total Price Before Sending to VNPay: {TotalPriceValue}", totalPriceValue);
            string ipAddress = HttpContext.Connection.RemoteIpAddress.ToString();
            ipAddress = (ipAddress == "::1") ? "127.0.0.1" : ipAddress;
            _logger.LogInformation("IP Address: {IpAddress}", ipAddress);

            // Gọi VNPay để tạo URL thanh toán
            string paymentUrl = _vnPayService.CreatePaymentUrl(order, ipAddress);
            _logger.LogInformation("Generated Payment URL: {PaymentUrl}", paymentUrl);


            await _context.SaveChangesAsync();

            return Ok(new { OrderId = order.OrderId, TotalPrice = order.TotalPrice, PaymentUrl = paymentUrl });
        }
    }
}
