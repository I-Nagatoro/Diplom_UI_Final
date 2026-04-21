using Avalonia.Controls;
using Avalonia.Interactivity;
using Diplom.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Diplom;

public partial class ActiveOrdersWindow : Window
{
    private readonly User _currentUser;

    public ActiveOrdersWindow() => InitializeComponent();

    public ActiveOrdersWindow(User user)
    {
        InitializeComponent();
        _currentUser = user;
        LoadOrders();
    }

    private async void LoadOrders()
    {
        await using var db = new DiplomContext();
        var activeOrders = await db.Orders
            .Where(o => !o.Completed && o.UserId == _currentUser.UserId)
            .Include(o => o.User)
            .ToListAsync();

        OrdersList.ItemsSource = activeOrders;
        EmptyText.IsVisible = !activeOrders.Any();
    }

    private async void OpenOrder(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not Order order) return;

        Window window;
        if (!string.IsNullOrEmpty(order.VideoPath))
            window = new LocalUploadWindow(order);
        else
            window = new NetworkUploadWindow();

        await window.ShowDialog(this);
        LoadOrders();
    }

    private void BackClick(object? sender, RoutedEventArgs e) => Close();
}