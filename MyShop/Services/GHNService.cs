using MyShop.Entities;
using Newtonsoft.Json;

namespace MyShop.Services
{
    public class MockGHNService : IGHNService
    {
        public Task<HttpResponseMessage> CalculateShippingFeeAsync(string userAddress, string sellerAddress)
        {
            // Tạo phản hồi giả lập
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"total_fee\": 30000}", System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }

        public Task<Dictionary<string, object>> GetShippingFeeAsync(string userAddress, string sellerAddress)
        {
            // Trả về dữ liệu giả lập dưới dạng Dictionary
            var feeDetails = new Dictionary<string, object>
    {
        { "total_fee", 30000 }
    };
            return Task.FromResult((dynamic)feeDetails);
        }
    }
}