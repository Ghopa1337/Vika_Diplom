using CargoTransport.Desktop.Services;
using System.Collections.ObjectModel;

namespace CargoTransport.Desktop.ViewModels;

public sealed class AdminNavigationItemViewModel : ViewModelBase
{
    private string _badge;

    public AdminNavigationItemViewModel(string title, string subtitle, string badge, object content)
    {
        Title = title;
        Subtitle = subtitle;
        _badge = badge;
        Content = content;
    }

    public string Title { get; }
    public string Subtitle { get; }
    public object Content { get; }

    public string Badge
    {
        get => _badge;
        set => Set(ref _badge, value);
    }
}

public sealed class AdminStatTileViewModel
{
    public AdminStatTileViewModel(string title, string value, string hint)
    {
        Title = title;
        Value = value;
        Hint = hint;
    }

    public string Title { get; }
    public string Value { get; }
    public string Hint { get; }
}

public sealed class AdminActivityItemViewModel
{
    public AdminActivityItemViewModel(string time, string title, string description)
    {
        Time = time;
        Title = title;
        Description = description;
    }

    public string Time { get; }
    public string Title { get; }
    public string Description { get; }
}

public sealed class AdminQuickActionCardViewModel
{
    public AdminQuickActionCardViewModel(string title, string description, string caption)
    {
        Title = title;
        Description = description;
        Caption = caption;
    }

    public string Title { get; }
    public string Description { get; }
    public string Caption { get; }
}

public sealed class AdminUserRowViewModel
{
    public required string Username { get; init; }
    public required string FullName { get; init; }
    public required string Role { get; init; }
    public required string Status { get; init; }
    public required string Phone { get; init; }
    public required string LastLogin { get; init; }
}

public sealed class AdminOrderRowViewModel
{
    public required string OrderNumber { get; init; }
    public required string Receiver { get; init; }
    public required string Driver { get; init; }
    public required string Vehicle { get; init; }
    public required string Status { get; init; }
    public required string DeliveryDate { get; init; }
}

public sealed class AdminDriverRowViewModel
{
    public required string FullName { get; init; }
    public required string Status { get; init; }
    public required string LicenseNumber { get; init; }
    public required string Experience { get; init; }
    public required string Phone { get; init; }
    public required string CurrentOrder { get; init; }
}

public sealed class AdminVehicleRowViewModel
{
    public required string LicensePlate { get; init; }
    public required string Model { get; init; }
    public required string BodyType { get; init; }
    public required string Capacity { get; init; }
    public required string Status { get; init; }
    public required string AssignedDriver { get; init; }
}

public sealed class AdminReportCardViewModel
{
    public AdminReportCardViewModel(string title, string description, string freshness, string format)
    {
        Title = title;
        Description = description;
        Freshness = freshness;
        Format = format;
    }

    public string Title { get; }
    public string Description { get; }
    public string Freshness { get; }
    public string Format { get; }
}

public sealed class AdminDashboardSectionViewModel
{
    public AdminDashboardSectionViewModel()
    {
        QuickActions = new ObservableCollection<AdminQuickActionCardViewModel>
        {
            new("Создать пользователя", "Быстрый старт для регистрации нового диспетчера, получателя или водителя.", "Следующий этап: форма CRUD"),
            new("Открыть заказы", "Переход в раздел с фильтрами по статусам, датам и получателям.", "Нужны таблица, фильтры и карточка заказа"),
            new("Проверить парк ТС", "Контроль доступности транспорта и распределения по рейсам.", "Нужны статусы и закрепление за водителем")
        };
    }

    public ObservableCollection<AdminStatTileViewModel> Metrics { get; } = [];
    public ObservableCollection<AdminActivityItemViewModel> RecentActivities { get; } = [];
    public ObservableCollection<AdminQuickActionCardViewModel> QuickActions { get; }

    public void ApplyData(AdminDashboardData data)
    {
        AdminCollectionHelper.ReplaceWith(
            Metrics,
            data.Metrics.Select(x => new AdminStatTileViewModel(x.Title, x.Value, x.Hint)));

        AdminCollectionHelper.ReplaceWith(
            RecentActivities,
            data.RecentActivities.Select(x => new AdminActivityItemViewModel(x.Time, x.Title, x.Description)));
    }
}

public sealed class AdminUsersSectionViewModel
{
    public ObservableCollection<AdminStatTileViewModel> Stats { get; } = [];
    public ObservableCollection<AdminUserRowViewModel> Users { get; } = [];

    public void ApplyData(AdminUsersData data)
    {
        AdminCollectionHelper.ReplaceWith(
            Stats,
            data.Stats.Select(x => new AdminStatTileViewModel(x.Title, x.Value, x.Hint)));

        AdminCollectionHelper.ReplaceWith(
            Users,
            data.Users.Select(x => new AdminUserRowViewModel
            {
                Username = x.Username,
                FullName = x.FullName,
                Role = x.Role,
                Status = x.Status,
                Phone = x.Phone,
                LastLogin = x.LastLogin
            }));
    }
}

public sealed class AdminOrdersSectionViewModel
{
    public ObservableCollection<AdminStatTileViewModel> Stats { get; } = [];
    public ObservableCollection<AdminOrderRowViewModel> Orders { get; } = [];

    public void ApplyData(AdminOrdersData data)
    {
        AdminCollectionHelper.ReplaceWith(
            Stats,
            data.Stats.Select(x => new AdminStatTileViewModel(x.Title, x.Value, x.Hint)));

        AdminCollectionHelper.ReplaceWith(
            Orders,
            data.Orders.Select(x => new AdminOrderRowViewModel
            {
                OrderNumber = x.OrderNumber,
                Receiver = x.Receiver,
                Driver = x.Driver,
                Vehicle = x.Vehicle,
                Status = x.Status,
                DeliveryDate = x.DeliveryDate
            }));
    }
}

public sealed class AdminDriversSectionViewModel
{
    public ObservableCollection<AdminStatTileViewModel> Stats { get; } = [];
    public ObservableCollection<AdminDriverRowViewModel> Drivers { get; } = [];

    public void ApplyData(AdminDriversData data)
    {
        AdminCollectionHelper.ReplaceWith(
            Stats,
            data.Stats.Select(x => new AdminStatTileViewModel(x.Title, x.Value, x.Hint)));

        AdminCollectionHelper.ReplaceWith(
            Drivers,
            data.Drivers.Select(x => new AdminDriverRowViewModel
            {
                FullName = x.FullName,
                Status = x.Status,
                LicenseNumber = x.LicenseNumber,
                Experience = x.Experience,
                Phone = x.Phone,
                CurrentOrder = x.CurrentOrder
            }));
    }
}

public sealed class AdminVehiclesSectionViewModel
{
    public ObservableCollection<AdminStatTileViewModel> Stats { get; } = [];
    public ObservableCollection<AdminVehicleRowViewModel> Vehicles { get; } = [];

    public void ApplyData(AdminVehiclesData data)
    {
        AdminCollectionHelper.ReplaceWith(
            Stats,
            data.Stats.Select(x => new AdminStatTileViewModel(x.Title, x.Value, x.Hint)));

        AdminCollectionHelper.ReplaceWith(
            Vehicles,
            data.Vehicles.Select(x => new AdminVehicleRowViewModel
            {
                LicensePlate = x.LicensePlate,
                Model = x.Model,
                BodyType = x.BodyType,
                Capacity = x.Capacity,
                Status = x.Status,
                AssignedDriver = x.AssignedDriver
            }));
    }
}

public sealed class AdminReportsSectionViewModel
{
    public ObservableCollection<AdminStatTileViewModel> Stats { get; } = [];
    public ObservableCollection<AdminReportCardViewModel> Reports { get; } = [];

    public void ApplyData(AdminReportsData data)
    {
        AdminCollectionHelper.ReplaceWith(
            Stats,
            data.Stats.Select(x => new AdminStatTileViewModel(x.Title, x.Value, x.Hint)));

        AdminCollectionHelper.ReplaceWith(
            Reports,
            data.Reports.Select(x => new AdminReportCardViewModel(x.Title, x.Description, x.Freshness, x.Format)));
    }
}

internal static class AdminCollectionHelper
{
    public static void ReplaceWith<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();

        foreach (T item in items)
        {
            collection.Add(item);
        }
    }
}
