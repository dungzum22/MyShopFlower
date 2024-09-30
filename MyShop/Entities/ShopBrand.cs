using System;
using System.Collections.Generic;

namespace MyShop.Entities;

public partial class ShopBrand
{
    public int ShopBrandId { get; set; }

    public int SellerId { get; set; }

    public string? Type { get; set; }

    public string ShopName { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public string? Introduction { get; set; }

    public DateTime? UpdateAt { get; set; }

    public int? Quantity { get; set; }

    public virtual ShopInfo Seller { get; set; } = null!;
}
