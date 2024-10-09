using System;
using System.Collections.Generic;

namespace MyShop.Entities;

public partial class Order
{
    public int OrderId { get; set; }

    public string? FlowerName { get; set; }

    public int? SellerId { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public int? UserId { get; set; }

    public string? PhoneNumber { get; set; }

    public string ShippingAddress { get; set; } = null!;

    public int? PaymentMethodId { get; set; }

    public string DeliveryMethod { get; set; } = null!;

    public double? Voucher { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual ICollection<OrdersDetail> OrdersDetails { get; set; } = new List<OrdersDetail>();

    public virtual PaymentMethod? PaymentMethod { get; set; }

    public virtual Seller? Seller { get; set; }

    public virtual User? User { get; set; }
}
