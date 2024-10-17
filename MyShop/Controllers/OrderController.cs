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

                // Kiểm tra xem người dùng có cung cấp voucher cho shop này không
                decimal discountAmount = 0;
                int? appliedVoucherStatusId = null; // Để lưu user_voucher_status_id đã được sử dụng cho nhóm sản phẩm này
                if (request.VoucherIds != null && request.VoucherIds.Any())
                {
                    // Lấy voucher của shop này từ danh sách voucher IDs
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
                            // Áp dụng giảm giá cho tổng giá trị của sản phẩm của shop này
                            var groupTotalPrice = group.Sum(item => item.Flower.Price * item.Quantity);
                            discountAmount = groupTotalPrice * (decimal)voucher.Discount / 100;
                            appliedVoucherStatusId = voucher.UserVoucherStatusId;

                            // Giảm số lần sử dụng còn lại của voucher
                            voucher.RemainingCount -= 1;
                            voucher.UsageCount += 1;

                            // Cập nhật vào database
                            _context.UserVoucherStatuses.Update(voucher);
                        }
                    }
                }

                // Tạo chi tiết đơn hàng cho từng sản phẩm trong nhóm và phân bổ giảm giá
                decimal groupTotalPriceBeforeDiscount = group.Sum(item => item.Flower.Price * item.Quantity);
                decimal discountAllocated = 0;

                foreach (var item in group)
                {
                    var itemPrice = item.Flower.Price * item.Quantity;
                    decimal itemDiscount = 0;

                    // Phân bổ giảm giá cho từng sản phẩm
                    if (groupTotalPriceBeforeDiscount > 0 && discountAmount > 0)
                    {
                        itemDiscount = (itemPrice / groupTotalPriceBeforeDiscount) * discountAmount;
                        discountAllocated += itemDiscount;
                    }

                    // Tạo chi tiết đơn hàng cho sản phẩm
                    var orderDetail = new OrdersDetail
                    {
                        OrderId = order.OrderId,
                        SellerId = item.Flower.SellerId,
                        FlowerId = item.FlowerId,
                        Price = (itemPrice - itemDiscount) + (isFirstItemInGroup ? shippingFee : 0), // Thêm phí giao hàng vào sản phẩm đầu tiên của nhóm
                        Amount = item.Quantity,
                        Status = "pending",
                        CreatedAt = DateTime.Now,
                        AddressId = request.AddressId,
                        DeliveryMethod = "Giao Hang Nhanh",
                        UserVoucherStatusId = appliedVoucherStatusId // Lưu user_voucher_status_id vào OrdersDetail
                    };

                    totalProductPrice += itemPrice - itemDiscount;
                    isFirstItemInGroup = false;

                    _context.OrdersDetails.Add(orderDetail);
                }

                // Điều chỉnh giá trị giảm để không có sai lệch do làm tròn
                if (discountAllocated != discountAmount)
                {
                    var lastOrderDetail = _context.OrdersDetails.LastOrDefault(od => od.OrderId == order.OrderId);
                    if (lastOrderDetail != null)
                    {
                        lastOrderDetail.Price -= (discountAllocated - discountAmount);
                    }
                }
            }

            // Tính tổng giá trị đơn hàng bao gồm cả phí giao hàng
            order.TotalPrice = totalProductPrice + totalShippingFee;

            await _context.SaveChangesAsync();

            return Ok(new { OrderId = order.OrderId, TotalPrice = order.TotalPrice });
        }



    }
}