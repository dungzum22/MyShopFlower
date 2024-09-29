using System;
using System.Collections.Generic;

namespace MyShop.Entities;

public partial class ShopBrand
{
    public int BrandId { get; set; }

    public int SellerId { get; set; }

    public string BrandName { get; set; } = null!;

    public string? BrandDescription { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? Quantity { get; set; }

    public virtual Seller Seller { get; set; } = null!;
}
