using Avalonia.Controls;
using Avalonia.Interactivity;
using Diplom.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Diplom;

public partial class ActiveOrdersWindow : Window
{
    private User _user;
    private List<Order> _orders;

    public ActiveOrdersWindow(User user)
    {
        InitializeComponent();
        _user = user;
        LoadOrders();
    }

    public ActiveOrdersWindow()
    {
        InitializeComponent();
    }

    private async void LoadOrders()
    {
        using var db = new DiplomContext();
        var activeOrders = await db.Orders
            .Where(o => !o.Completed
                   && o.UserId == _user.UserId)
            .ToListAsync();

        OrdersList.ItemsSource = activeOrders;

        if (!activeOrders.Any())
        {
            EmptyText.IsVisible = true;
        }
        else
        {
            EmptyText.IsVisible = false;
        }
    }

    private async void OpenOrder(object? sender, RoutedEventArgs e)
    {
        var order = (sender as Button)?.DataContext as Order;
        if (order == null) return;

        Window window;

        // 🔥 ЛОГИКА ВЫБОРА
        if (!string.IsNullOrEmpty(order.VideoPath))
            window = new LocalUploadWindow(order);
        else
            window = new NetworkUploadWindow();

        await window.ShowDialog(this);
    }

    private void BackClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}