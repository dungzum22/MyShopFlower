namespace MyShop.DTO
{
    public class RegisterSellerDto
    {
        public string ShopName { get; set; } = string.Empty;
        public string Introduction { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // individual hoặc enterprise
    }
}
