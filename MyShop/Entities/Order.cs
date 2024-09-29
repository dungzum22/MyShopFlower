using System;
using System.Collections.Generic;

namespace MyShop.Entities;

public partial class Order
{
    public int OrderId { get; set; }

    public string? FlowerName { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public int? UserId { get; set; }

    public int? PhoneNumber { get; set; }

    public string ShippingAddress { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public string DeliveryMethod { get; set; } = null!;

    public string? Status { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual ICollection<OrdersDetail> OrdersDetails { get; set; } = new List<OrdersDetail>();

    public virtual User? User { get; set; }
}
