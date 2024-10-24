namespace MyShop.DTO
{
    public class UpdateSellerDto
    {
        public string ShopName { get; set; }
        public string AddressSeller { get; set; }
        public string Introduction { get; set; }
        public string Role { get; set; } // Vai trò (ví dụ: enterprise, individual)
    }
}
