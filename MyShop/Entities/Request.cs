using System;
using System.Collections.Generic;

namespace MyShop.Entities;

public partial class Request
{
    public int RequestId { get; set; }

    public int? UserInfoId { get; set; }

    public int? SellerId { get; set; }

    public int? Points { get; set; }

    public int? Price { get; set; }

    public string? Description { get; set; }

    public virtual Seller? Seller { get; set; }

    public virtual UserInfo? UserInfo { get; set; }
}
