using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public string? VideoUri { get; set; }

    public string? VideoPath { get; set; }

    public DateTime DatetimeOrder { get; set; }

    public int UserId { get; set; }

    public virtual User User { get; set; } = null!;
}
