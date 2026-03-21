using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class History
{
    public int HistoryRecordId { get; set; }

    public int UserId { get; set; }

    public DateTime DatetimeOrder { get; set; }

    public DateTime DatetimeFinish { get; set; }

    public string? VideoUri { get; set; }

    public string? VideoPath { get; set; }

    public virtual User User { get; set; } = null!;
}
