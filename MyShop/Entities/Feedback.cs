using System;
using System.Collections.Generic;

namespace MyShop.Entities;

public partial class Feedback
{
    public int FeedbackId { get; set; }

    public int ReviewId { get; set; }

    public int SellerId { get; set; }

    public string FeedbackText { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Review Review { get; set; } = null!;

    public virtual Seller Seller { get; set; } = null!;
}
