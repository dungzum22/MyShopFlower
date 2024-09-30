using System;
using System.Collections.Generic;

namespace MyShop.Entities;

public partial class ShopInfo
{
    public int SellerId { get; set; }

    public int UserId { get; set; }

    public string ShopName { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public int? TotalProduct { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ShopBrand> ShopBrands { get; set; } = new List<ShopBrand>();

    public virtual User User { get; set; } = null!;
}
