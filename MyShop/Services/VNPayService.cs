using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using MyShop.Entities;

namespace MyShop.Services
{
    public class VNPayService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<VNPayService> _logger;

        public VNPayService(IConfiguration configuration, ILogger<VNPayService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string CreatePaymentUrl(Order order, string ipAddress)
        {
            string vnp_TmnCode = _configuration["VNPay:TmnCode"];
            string vnp_HashSecret = _configuration["VNPay:HashSecret"];
            string vnp_Url = _configuration["VNPay:ApiUrl"];
            string vnp_ReturnUrl = _configuration["VNPay:ReturnUrl"];

            // Ghi log thông tin trước khi tạo URL
            _logger.LogInformation($"Creating VNPay request with IP Address: {ipAddress}");

            var vnp_Params = new SortedList<string, string>();
            vnp_Params.Add("vnp_Version", "2.1.0");
            vnp_Params.Add("vnp_Command", "pay");
            vnp_Params.Add("vnp_TmnCode", vnp_TmnCode);
            vnp_Params.Add("vnp_Amount", ((int)(order.TotalPrice * 100)).ToString());
            vnp_Params.Add("vnp_CreateDate", order.CreatedDate?.ToString("yyyyMMddHHmmss"));
            vnp_Params.Add("vnp_CurrCode", "VND");
            vnp_Params.Add("vnp_IpAddr", ipAddress);
            vnp_Params.Add("vnp_Locale", "vn");
            vnp_Params.Add("vnp_OrderInfo", $"Thanh toan don hang: {order.OrderId}");
            vnp_Params.Add("vnp_OrderType", "billpayment");
            vnp_Params.Add("vnp_ReturnUrl", vnp_ReturnUrl);
            vnp_Params.Add("vnp_TxnRef", order.OrderId.ToString());

            StringBuilder data = new StringBuilder();
            foreach (var kv in vnp_Params)
            {
                data.Append($"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}&");
            }
            string queryString = data.ToString().TrimEnd('&');
            string secureHash = HmacSHA512(vnp_HashSecret, queryString);
            string paymentUrl = $"{vnp_Url}?{queryString}&vnp_SecureHash={secureHash}";

            // Ghi log thông tin URL thanh toán cuối cùng
            _logger.LogInformation("Generated payment URL: {PaymentUrl}", paymentUrl);

            return paymentUrl;
        }

        public bool ValidateSignature(SortedList<string, string> vnpayData, string inputHash, string secretKey)
        {
            // Xóa các tham số chữ ký khỏi dữ liệu cần xác thực
            vnpayData.Remove("vnp_SecureHashType");
            vnpayData.Remove("vnp_SecureHash");

            StringBuilder data = new StringBuilder();
            foreach (var kv in vnpayData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append($"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}&");
                }
            }
            string dataToHash = data.ToString().TrimEnd('&');
            string myChecksum = HmacSHA512(secretKey, dataToHash);

            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        public string HmacSHA512(string key, string inputData)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key), "Key không được null hoặc trống");
            }
            if (string.IsNullOrEmpty(inputData))
            {
                throw new ArgumentNullException(nameof(inputData), "InputData không được null hoặc trống");
            }

            using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(inputData));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}
