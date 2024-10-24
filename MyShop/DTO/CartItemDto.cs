namespace MyShop.DTO
{
    public class CartItemDto
    {
        public int? FlowerId { get; set; }
        public string FlowerName { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
