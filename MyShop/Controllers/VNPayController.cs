//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using MyShop.DataContext;
//using MyShop.Services;

//namespace MyShop.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    [Authorize]
//    public class VNPayController : ControllerBase
//    {
//        private readonly FlowershopContext _context;
//        private readonly IVNPayService _vnpayService;
//        private readonly ILogger<VNPayController> _logger;

//        public VNPayController(FlowershopContext context, IVNPayService vnpayService, ILogger<VNPayController> logger)
//        {
//            _context = context;
//            _vnpayService = vnpayService;
//            _logger = logger;
//        }

//        [HttpPost("process-payment")]
//        public async Task<IActionResult> ProcessPayment([FromBody] int orderId)
//        {
//            // Lấy đơn hàng từ cơ sở dữ liệu
//            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
//            if (order == null)
//            {
//                return NotFound("Không tìm thấy đơn hàng.");
//            }

//            if (order.Status != "pending")
//            {
//                return BadRequest("Đơn hàng không ở trạng thái hợp lệ để thanh toán.");
//            }

//            // Xử lý thanh toán VNPay
//            try
//            {
//                var transactionId = await _vnpayService.ProcessPaymentAsync(order.TotalPrice, order.PhoneNumber);
//                order.TransactionId = transactionId;
//                order.Status = "paid";
//                await _context.SaveChangesAsync();
//                return Ok(order);
//            }
//            catch (HttpRequestException ex)
//            {
//                _logger.LogError("Thanh toán thất bại: {Message}", ex.Message);
//                return BadRequest($"Thanh toán thất bại. Chi tiết lỗi: {ex.Message}");
//            }
//        }
//        [HttpGet("return")]
//        public IActionResult ReturnUrl()
//        {
//            // Xử lý giả lập cho Swagger để ghi log khi thanh toán hoàn tất
//            _logger.LogInformation("Redirected to return URL after VNPay payment.");
//            return Ok("Thanh toán đã hoàn tất (giả lập).");
//        }

//    }
//}
//}
