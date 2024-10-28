using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using MyShop.DataContext;
using MyShop.Services;
using System.Threading.Tasks;
using MyShop.Services;
using System.Net;

namespace MyShop.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VNPayController : ControllerBase
    {
        private readonly FlowershopContext _context;
        private readonly IConfiguration _configuration;
        private readonly VNPayService _vnPayService;
        private readonly ILogger<VNPayController> _logger;

        public VNPayController(FlowershopContext context, IConfiguration configuration, VNPayService vnPayService, ILogger<VNPayController> logger)
        {
            _context = context;
            _configuration = configuration;
            _vnPayService = vnPayService;
            _logger = logger;
        }

        [HttpGet("vnpay_return")]
        public IActionResult VNPayReturn()
        {
            var queryParameters = HttpContext.Request.Query;
            _logger.LogInformation("Query parameters received from VNPay: {QueryParameters}", queryParameters);
            var vnpayData = new SortedList<string, string>();

            foreach (var key in queryParameters.Keys)
            {
                if (key.StartsWith("vnp_"))
                {
                    vnpayData.Add(key, queryParameters[key]);
                }
            }

            // Lấy thông tin từ appsettings.json
            string vnp_HashSecret = _configuration["VNPay:HashSecret"];

            // Tạo chuỗi dữ liệu để kiểm tra chữ ký
            string rawData = string.Join("&", vnpayData
     .Where(x => x.Key != "vnp_SecureHash")
     .OrderBy(x => x.Key)
     .Select(x => $"{WebUtility.UrlEncode(x.Key)}={WebUtility.UrlEncode(x.Value)}"));


            string secureHash = vnpayData["vnp_SecureHash"];
            string calculatedHash = _vnPayService.HmacSHA512(vnp_HashSecret, rawData);

            _logger.LogInformation("Raw data for hash calculation: {RawData}", rawData);
            _logger.LogInformation("Calculated hash: {CalculatedHash}", calculatedHash);
            _logger.LogInformation("Received secure hash: {SecureHash}", secureHash);

            if (calculatedHash.Equals(secureHash, System.StringComparison.OrdinalIgnoreCase))
            {
                // Kiểm tra mã đơn hàng và cập nhật trạng thái đơn hàng
                var orderId = int.Parse(vnpayData["vnp_TxnRef"]);
                var order = _context.Orders.FirstOrDefault(o => o.OrderId == orderId);
                if (order != null)
                {
                    // Cập nhật trạng thái đơn hàng
                    order.Status = vnpayData["vnp_ResponseCode"] == "00" ? "paid" : "failed";
                    _context.Orders.Update(order);
                    _context.SaveChanges();
                }

                return Ok("Giao dịch hoàn tất");
            }
            else
            {
                _logger.LogError("Signature verification failed for transaction: {TxnRef}", vnpayData["vnp_TxnRef"]);
                return BadRequest("Chữ ký không hợp lệ");
            }
        }
    }
}