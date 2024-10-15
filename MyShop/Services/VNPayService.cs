using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;


using Newtonsoft.Json;

namespace MyShop.Services
{
    public class VNPayService : IVNPayService
    {
        private readonly HttpClient _httpClient;

        public VNPayService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> ProcessPaymentAsync(decimal amount, string phoneNumber)
        {
            var vnpayRequest = new
            {
                amount = amount,
                order_info = "Thanh toán đơn hàng từ Flower Shop",
                phone_number = phoneNumber
            };

            var content = new StringContent(JsonConvert.SerializeObject(vnpayRequest), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://vnpay.vn/api/payment", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException("Thanh toán thất bại!");
            }

            // Giả sử phản hồi từ VNPay chứa `transaction_id` trong nội dung.
            var transactionId = await response.Content.ReadAsStringAsync();
            return transactionId;
        }
    }

}
