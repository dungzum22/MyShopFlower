namespace MyShop.DTO
{
    public class FlowerDto
    {
        public string? FlowerName { get; set; }
        public string? FlowerDescription { get; set; }
        public decimal Price { get; set; }
        public int AvailableQuantity { get; set; }
        public int CategoryId { get; set; }
        public int SellerId { get; set; }
        public IFormFile Image { get; set; } // For image upload
    }
}
