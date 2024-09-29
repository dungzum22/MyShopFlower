using System;
using System.Collections.Generic;

namespace MyShop.Entities;

public partial class Cart
{
    public int CartId { get; set; }

    public int? UserId { get; set; }

    public int? FlowerInfoId { get; set; }

    public int Quantity { get; set; }

    public virtual FlowerInfo? FlowerInfo { get; set; }

    public virtual User? User { get; set; }
}
