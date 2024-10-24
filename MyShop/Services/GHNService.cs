using MyShop.Entities;
using Newtonsoft.Json;

namespace MyShop.Services
{
    public class MockGHNService : IGHNService
    {
        // Bảng phí vận chuyển giả lập cho các tỉnh thành
        private readonly Dictionary<string, int> _provinceShippingFees = new Dictionary<string, int>
        {
            { "Hà Nội", 20000 },
            { "TP Hồ Chí Minh", 25000 },
            { "Đà Nẵng", 22000 },
            { "Hải Phòng", 24000 },
            { "Cần Thơ", 26000 },
            { "An Giang", 27000 },
            { "Bà Rịa - Vũng Tàu", 23000 },
            { "Bắc Giang", 21000 },
            { "Bắc Kạn", 28000 },
            { "Bạc Liêu", 29000 },
            { "Bắc Ninh", 20000 },
            { "Bến Tre", 27000 },
            { "Bình Định", 25000 },
            { "Bình Dương", 24000 },
            { "Bình Phước", 26000 },
            { "Bình Thuận", 25000 },
            { "Cà Mau", 28000 },
            { "Cao Bằng", 29000 },
            { "Đắk Lắk", 27000 },
            { "Đắk Nông", 27000 },
            { "Điện Biên", 30000 },
            { "Đồng Nai", 24000 },
            { "Đồng Tháp", 25000 },
            { "Gia Lai", 26000 },
            { "Hà Giang", 30000 },
            { "Hà Nam", 20000 },
            { "Hà Tĩnh", 25000 },
            { "Hậu Giang", 27000 },
            { "Hòa Bình", 21000 },
            { "Hưng Yên", 20000 },
            { "Khánh Hòa", 23000 },
            { "Kiên Giang", 28000 },
            { "Kon Tum", 27000 },
            { "Lai Châu", 30000 },
            { "Lâm Đồng", 25000 },
            { "Lạng Sơn", 24000 },
            { "Lào Cai", 29000 },
            { "Long An", 22000 },
            { "Nam Định", 21000 },
            { "Nghệ An", 25000 },
            { "Ninh Bình", 22000 },
            { "Ninh Thuận", 24000 },
            { "Phú Thọ", 22000 },
            { "Phú Yên", 26000 },
            { "Quảng Bình", 25000 },
            { "Quảng Nam", 23000 },
            { "Quảng Ngãi", 25000 },
            { "Quảng Ninh", 24000 },
            { "Quảng Trị", 25000 },
            { "Sóc Trăng", 28000 },
            { "Sơn La", 29000 },
            { "Tây Ninh", 24000 },
            { "Thái Bình", 21000 },
            { "Thái Nguyên", 22000 },
            { "Thanh Hóa", 25000 },
            { "Thừa Thiên Huế", 23000 },
            { "Tiền Giang", 22000 },
            { "Trà Vinh", 27000 },
            { "Tuyên Quang", 28000 },
            { "Vĩnh Long", 27000 },
            { "Vĩnh Phúc", 21000 },
            { "Yên Bái", 28000 }
        };

        // Hàm lấy tỉnh/thành phố từ địa chỉ
        private string GetProvinceFromAddress(string address)
        {
            // Giả sử địa chỉ có định dạng: "Số nhà, Đường, Phường/Xã, Quận/Huyện, Tỉnh/Thành phố"
            var addressParts = address.Split(',');
            if (addressParts.Length > 0)
            {
                var province = addressParts.Last().Trim();
                return province;
            }
            return string.Empty;
        }


        public Task<HttpResponseMessage> CalculateShippingFeeAsync(string userAddress, string sellerAddress)
        {
            // Lấy tỉnh thành từ địa chỉ
            var userProvince = GetProvinceFromAddress(userAddress);
            var sellerProvince = GetProvinceFromAddress(sellerAddress);

            // Mặc định phí vận chuyển nếu không tìm thấy tỉnh thành
            int shippingFee = 30000;

            // Kiểm tra xem tỉnh thành của người dùng có trong danh sách hay không
            if (_provinceShippingFees.ContainsKey(userProvince))
            {
                shippingFee = _provinceShippingFees[userProvince];
            }

            // Tạo phản hồi giả lập với phí vận chuyển
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(new { total_fee = shippingFee }), System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }

        public Task<Dictionary<string, object>> GetShippingFeeAsync(string userAddress, string sellerAddress)
        {
            // Lấy tỉnh thành từ địa chỉ
            var userProvince = GetProvinceFromAddress(userAddress);
            var sellerProvince = GetProvinceFromAddress(sellerAddress);

            // Mặc định phí vận chuyển nếu không tìm thấy tỉnh thành
            int shippingFee = 30000;

            // Kiểm tra xem tỉnh thành của người mua có trong danh sách không
            if (_provinceShippingFees.ContainsKey(userProvince))
            {
                shippingFee = _provinceShippingFees[userProvince];
            }

            // Trả về dữ liệu giả lập dưới dạng Dictionary
            var feeDetails = new Dictionary<string, object>
            {{ "total_fee", shippingFee }
            };

            return Task.FromResult((dynamic)feeDetails);
        }
    }
}
