using System;
using System.Collections.Generic;

namespace MyShop.Entities;

public partial class Seller
{
    public int SellerId { get; set; }

    public int UserId { get; set; }

    public string ShopName { get; set; } = null!;

    public string? Type { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? TotalProduct { get; set; }

    public string Role { get; set; } = null!;

    public string? Introduction { get; set; }

    public int? Quantity { get; set; }

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    public virtual User User { get; set; } = null!;
}
