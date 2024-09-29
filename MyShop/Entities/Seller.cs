using System;
using System.Collections.Generic;

namespace MyShop.Entities;

public partial class Seller
{
    public int SellerId { get; set; }

    public int UserId { get; set; }

    public string SellerType { get; set; } = null!;

    public string? BusinessName { get; set; }

    public string? TaxId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    public virtual ICollection<ShopBrand> ShopBrands { get; set; } = new List<ShopBrand>();

    public virtual User User { get; set; } = null!;
}
