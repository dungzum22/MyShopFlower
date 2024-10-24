using System;
using System.Collections.Generic;

namespace MyShop.Entities;

public partial class Review
{
    public int ReviewId { get; set; }

    public int UserId { get; set; }

    public int FlowerId { get; set; }

    public int OrderId { get; set; }

    public int Rating { get; set; }

    public string? ReviewText { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual FlowerInfo Flower { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
