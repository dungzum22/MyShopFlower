using System;
using System.Collections.Generic;

namespace MyShop.Entities;

public partial class Message
{
    public int MessageId { get; set; }

    public int SenderId { get; set; }

    public int RecipientId { get; set; }

    public string MessageText { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public string? Status { get; set; }

    public virtual User Recipient { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}
