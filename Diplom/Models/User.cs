using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class User
{
    public int UserId { get; set; }

    public string UserName { get; set; } = null!;

    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public int RoleId { get; set; }

    public string ImagePath { get; set; } = null!;
    public Bitmap Image { get
        {
            return new Bitmap(AppDomain.CurrentDomain.BaseDirectory + "/" + ImagePath);
        } }

    public virtual ICollection<History> Histories { get; set; } = new List<History>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual Role Role { get; set; } = null!;
}
