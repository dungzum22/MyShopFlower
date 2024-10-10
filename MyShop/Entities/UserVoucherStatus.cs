﻿using System;
using System.Collections.Generic;

namespace MyShop.Entities;

public partial class UserVoucherStatus
{
    public int UserVoucherStatusId { get; set; }

    public int? UserInfoId { get; set; }

    public string VoucherCode { get; set; } = null!;

    public double Discount { get; set; }

    public string? Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int? UsageLimit { get; set; }

    public int? UsageCount { get; set; }

    public int? RemainingCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<OrdersDetail> OrdersDetails { get; set; } = new List<OrdersDetail>();

    public virtual UserInfo? UserInfo { get; set; }
}