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

    public string PaymentMethod { get; set; } = null!;

    public string? TransactionId { get; set; }

    public string DeliveryMethod { get; set; } = null!;

    public string? Status { get; set; }

    public DateTime? CreatedDate { get; set; }

    public int? UserVoucherStatusId { get; set; }

    public int? AddressId { get; set; }

    public int? CartId { get; set; }

    public virtual Address? Address { get; set; }

    public virtual Cart? Cart { get; set; }

    public virtual ICollection<OrdersDetail> OrdersDetails { get; set; } = new List<OrdersDetail>();

    public virtual Seller? Seller { get; set; }

    public virtual User? User { get; set; }

    public virtual UserVoucherStatus? UserVoucherStatus { get; set; }
}
