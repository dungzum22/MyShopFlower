using Microsoft.AspNetCore.Mvc;

namespace MyShop.DTO
{
    public class OrderDetailDto
    {
        public int? OrderDetailId { get; set; }
        public int? OrderId { get; set; }
        public int? SellerId { get; set; }
        public string ShopName { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; }
        public int? AddressId { get; set; }
        public string AddressDescription { get; set; }
        public int? FlowerId { get; set; }
        public string FlowerName { get; set; }
        public string FlowerImage { get; set; }
        public decimal Price { get; set; }
        public int? Amount { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string DeliveryMethod { get; set; }
    }
}
