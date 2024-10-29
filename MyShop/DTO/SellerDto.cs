namespace MyShop.DTO
{
    public class SellerDto
    {
        public int SellerId { get; set; }
        public string ShopName { get; set; }
        public string AddressSeller { get; set; }
        public string Introduction { get; set; }
        public string Role { get; set; }
        public int? TotalProduct { get; set; }
        public int? Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
