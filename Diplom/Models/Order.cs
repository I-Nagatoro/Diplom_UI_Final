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

    public int? Progress { get; set; }

    public string? Stage { get; set; }

    public string? Status { get; set; }

    public bool Completed { get; set; }

    public string? TaskId { get; set; }

    public string? FileId { get; set; }

    public virtual User User { get; set; } = null!;
}
