﻿using System;
using System.Collections.Generic;

namespace MyShop.Entities;

public partial class Address
{
    public int UserInfoId { get; set; }

    public string? Description { get; set; }

    public virtual UserInfo UserInfo { get; set; } = null!;
}