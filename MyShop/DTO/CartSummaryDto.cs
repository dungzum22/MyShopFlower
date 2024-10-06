namespace MyShop.DTO
{
    public class CartSummaryDto
    {
        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
        public int TotalQuantity { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
