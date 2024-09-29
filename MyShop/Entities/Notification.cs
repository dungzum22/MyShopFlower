using System;
using System.Collections.Generic;

namespace MyShop.Entities;

public partial class Notification
{
    public int NotificationId { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public int? SenderId { get; set; }

    public int? RecipientId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? Recipient { get; set; }

    public virtual User? Sender { get; set; }
}
