using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using CargoTransport.Desktop.Models;
using CargoTransport.Desktop.Services;

namespace CargoTransport.Desktop.ViewModels;

public sealed class OrderStatusStepViewModel
{
    public OrderStatusStepViewModel(string title, string caption, bool isCompleted, bool isCurrent)
    {
        Title = title;
        Caption = caption;
        IsCompleted = isCompleted;
        IsCurrent = isCurrent;
    }

    public string Title { get; }
    public string Caption { get; }
    public bool IsCompleted { get; }
    public bool IsCurrent { get; }
    public string BadgeBackground => IsCompleted || IsCurrent ? "#0F5C69" : "#FFFFFF";
    public string BadgeBorderBrush => IsCompleted || IsCurrent ? "#0F5C69" : "#CAD6DE";
    public string BadgeForeground => IsCompleted || IsCurrent ? "#FFFFFF" : "#667582";
    public string TitleForeground => IsCurrent ? "#0F5C69" : "#22303A";
    public string ConnectorBrush => IsCompleted ? "#0F5C69" : "#D9E4EA";
}

public sealed class OrderTimelineItemViewModel
{
    public required string Time { get; init; }
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required string Actor { get; init; }
    public bool IsLatest { get; init; }
    public string DotBackground => IsLatest ? "#0F5C69" : "#D8E3E9";
}

public sealed class OrderDetailsViewModel : ViewModelBase
{
    private static readonly CultureInfo RuCulture = CultureInfo.GetCultureInfo("ru-RU");
    private readonly IAdminCrudService _adminCrudService;
    private readonly Func<Task> _onClosed;
    private Order _order;

    public OrderDetailsViewModel(
        IAdminCrudService adminCrudService,
        Order order,
        Func<Task> onClosed)
    {
        _adminCrudService = adminCrudService;
        _order = order;
        _onClosed = onClosed;

        UpdateStatusCommand = new AsyncRelayCommand(UpdateStatusAsync, CanUpdateStatus);
        CloseCommand = new AsyncRelayCommand(_onClosed);
    }

    public Order Order
    {
        get => _order;
        set => Set(ref _order, value);
    }

    public ObservableCollection<OrderStatusStepViewModel> StatusSteps { get; } = [];
    public ObservableCollection<OrderTimelineItemViewModel> Timeline { get; } = [];

    public ICommand UpdateStatusCommand { get; }
    public ICommand CloseCommand { get; }

    public string OrderNumber => Order.OrderNumber;
    public string StatusLabel => GetStatusName(Order.Status);
    public string StatusHint => GetStatusHint(Order.Status);
    public string CostLabel => Order.TotalCost.HasValue ? $"{Order.TotalCost.Value:0.##} ₽" : "Не указана";
    public string DistanceLabel => Order.DistanceKm.HasValue ? $"{Order.DistanceKm.Value:0.##} км" : "Не указано";
    public string CreatedAtLabel => FormatDateTime(Order.CreatedAt);
    public string UpdatedAtLabel => FormatDateTime(Order.UpdatedAt);
    public string ReceiverLabel => Order.ReceiverUser?.CompanyName ?? Order.ReceiverUser?.FullName ?? "Не указан";
    public string DriverLabel => Order.Driver?.User?.FullName ?? "Не назначен";
    public string VehicleLabel => Order.Vehicle?.LicensePlate ?? "Не назначен";
    public string CargoName => Order.Cargo?.Name ?? "Не указан";
    public string CargoTypeLabel => GetCargoTypeName(Order.Cargo?.CargoType);
    public string CargoMetrics => BuildCargoMetrics();
    public string CargoRequirements => string.IsNullOrWhiteSpace(Order.Cargo?.SpecialRequirements) ? "Без особых требований" : Order.Cargo.SpecialRequirements!;
    public string PickupLabel => BuildContactBlock("Погрузка", Order.PickupAddress, Order.PickupContactName, Order.PickupContactPhone);
    public string DeliveryLabel => BuildContactBlock("Доставка", Order.DeliveryAddress, Order.DeliveryContactName, Order.DeliveryContactPhone);
    public string ScheduleLabel => BuildScheduleLabel();
    public string CommentLabel => string.IsNullOrWhiteSpace(Order.Comment) ? "Комментарий не добавлен." : Order.Comment!;
    public string CancellationLabel => string.IsNullOrWhiteSpace(Order.CancellationReason) ? "Не отменён" : Order.CancellationReason!;
    public string HistorySummary => Timeline.Count == 0 ? "История изменений пока пуста." : $"Событий в истории: {Timeline.Count}";

    public bool CanDriverAccept => Order.Status == Order.OrderStatuses.Assigned;
    public bool CanDriverLoad => Order.Status == Order.OrderStatuses.Accepted;
    public bool CanDriverDispatch => Order.Status == Order.OrderStatuses.Loading;
    public bool CanDriverDeliver => Order.Status == Order.OrderStatuses.InTransit;
    public bool CanReceiverConfirm => Order.Status == Order.OrderStatuses.Delivered;

    public Task LoadDetailsAsync()
    {
        RebuildStatusSteps();
        RebuildTimeline();
        RaiseComputedProperties();
        return Task.CompletedTask;
    }

    private bool CanUpdateStatus() => GetNextStatus() is not null && Order.Id > 0;

    private async Task UpdateStatusAsync()
    {
        string? nextStatus = GetNextStatus();
        if (string.IsNullOrWhiteSpace(nextStatus) || Order.Id == 0)
        {
            return;
        }

        string oldStatus = Order.Status;
        var updatedOrder = new AdminOrderEditData(
            Order.Id,
            Order.OrderNumber,
            Order.ReceiverUserId,
            Order.CargoId,
            Order.DriverId,
            Order.VehicleId,
            Order.PickupAddress,
            Order.DeliveryAddress,
            Order.PickupContactName,
            Order.PickupContactPhone,
            Order.DeliveryContactName,
            Order.DeliveryContactPhone,
            Order.DistanceKm,
            Order.TotalCost,
            nextStatus,
            Order.PlannedPickupAt,
            Order.DesiredDeliveryAt,
            Order.CancellationReason,
            Order.Comment);

        await _adminCrudService.UpdateOrderAsync(updatedOrder);

        Order.Status = nextStatus;
        Order.UpdatedAt = DateTime.Now;
        Order.StatusHistory.Add(new OrderStatusHistory
        {
            OrderId = Order.Id,
            OldStatus = oldStatus,
            NewStatus = nextStatus,
            ChangedAt = DateTime.Now,
            ChangedByUserId = Order.CreatedByUserId,
            ChangedByUser = Order.CreatedByUser,
            Comment = GetStatusChangeComment(nextStatus)
        });

        await LoadDetailsAsync();
        (UpdateStatusCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    private void RebuildStatusSteps()
    {
        StatusSteps.Clear();

        string[] flow =
        [
            Order.OrderStatuses.Created,
            Order.OrderStatuses.Assigned,
            Order.OrderStatuses.Accepted,
            Order.OrderStatuses.Loading,
            Order.OrderStatuses.InTransit,
            Order.OrderStatuses.Delivered,
            Order.OrderStatuses.Received
        ];

        int currentIndex = Array.IndexOf(flow, Order.Status);
        for (int index = 0; index < flow.Length; index++)
        {
            string status = flow[index];
            bool isCompleted = currentIndex >= 0 && index < currentIndex;
            bool isCurrent = status == Order.Status;
            if (Order.Status == Order.OrderStatuses.Cancelled)
            {
                isCompleted = index == 0;
                isCurrent = false;
            }

            StatusSteps.Add(new OrderStatusStepViewModel(
                GetStatusName(status),
                GetStepCaption(status),
                isCompleted,
                isCurrent));
        }
    }

    private void RebuildTimeline()
    {
        Timeline.Clear();

        List<OrderStatusHistory> entries = Order.StatusHistory
            .OrderByDescending(x => x.ChangedAt)
            .ToList();

        for (int index = 0; index < entries.Count; index++)
        {
            OrderStatusHistory item = entries[index];
            Timeline.Add(new OrderTimelineItemViewModel
            {
                Time = FormatDateTime(item.ChangedAt),
                Title = BuildTimelineTitle(item),
                Subtitle = string.IsNullOrWhiteSpace(item.Comment) ? "Комментарий не добавлен." : item.Comment!,
                Actor = item.ChangedByUser?.FullName ?? $"Пользователь #{item.ChangedByUserId}",
                IsLatest = index == 0
            });
        }
    }

    private void RaiseComputedProperties()
    {
        OnPropertyChanged(nameof(OrderNumber));
        OnPropertyChanged(nameof(StatusLabel));
        OnPropertyChanged(nameof(StatusHint));
        OnPropertyChanged(nameof(CostLabel));
        OnPropertyChanged(nameof(DistanceLabel));
        OnPropertyChanged(nameof(CreatedAtLabel));
        OnPropertyChanged(nameof(UpdatedAtLabel));
        OnPropertyChanged(nameof(ReceiverLabel));
        OnPropertyChanged(nameof(DriverLabel));
        OnPropertyChanged(nameof(VehicleLabel));
        OnPropertyChanged(nameof(CargoName));
        OnPropertyChanged(nameof(CargoTypeLabel));
        OnPropertyChanged(nameof(CargoMetrics));
        OnPropertyChanged(nameof(CargoRequirements));
        OnPropertyChanged(nameof(PickupLabel));
        OnPropertyChanged(nameof(DeliveryLabel));
        OnPropertyChanged(nameof(ScheduleLabel));
        OnPropertyChanged(nameof(CommentLabel));
        OnPropertyChanged(nameof(CancellationLabel));
        OnPropertyChanged(nameof(HistorySummary));
        OnPropertyChanged(nameof(CanDriverAccept));
        OnPropertyChanged(nameof(CanDriverLoad));
        OnPropertyChanged(nameof(CanDriverDispatch));
        OnPropertyChanged(nameof(CanDriverDeliver));
        OnPropertyChanged(nameof(CanReceiverConfirm));
    }

    private string? GetNextStatus() =>
        Order.Status switch
        {
            Order.OrderStatuses.Assigned => Order.OrderStatuses.Accepted,
            Order.OrderStatuses.Accepted => Order.OrderStatuses.Loading,
            Order.OrderStatuses.Loading => Order.OrderStatuses.InTransit,
            Order.OrderStatuses.InTransit => Order.OrderStatuses.Delivered,
            Order.OrderStatuses.Delivered => Order.OrderStatuses.Received,
            _ => null
        };

    private static string GetStatusName(string status) =>
        status switch
        {
            "created" => "Создан",
            "assigned" => "Назначен",
            "accepted" => "Принят",
            "loading" => "Погрузка",
            "in_transit" => "В пути",
            "delivered" => "Доставлен",
            "received" => "Получен",
            "cancelled" => "Отменён",
            _ => status
        };

    private static string GetStatusHint(string status) =>
        status switch
        {
            "created" => "Заказ зарегистрирован и ждёт назначения экипажа.",
            "assigned" => "Водитель и транспорт назначены, ожидается подтверждение.",
            "accepted" => "Водитель подтвердил заказ и готовится к погрузке.",
            "loading" => "Груз проходит этап погрузки и подготовки к отправке.",
            "in_transit" => "Перевозка выполняется, заказ находится в пути.",
            "delivered" => "Груз доставлен и ожидает подтверждения получателя.",
            "received" => "Доставка закрыта, получатель подтвердил получение.",
            "cancelled" => "Заказ отменён и снят с активного маршрута.",
            _ => "Статус заказа обновлён."
        };

    private static string GetStepCaption(string status) =>
        status switch
        {
            "created" => "регистрация",
            "assigned" => "назначение",
            "accepted" => "подтверждение",
            "loading" => "подготовка",
            "in_transit" => "маршрут",
            "delivered" => "прибытие",
            "received" => "закрытие",
            _ => status
        };

    private static string GetCargoTypeName(string? cargoType) =>
        cargoType switch
        {
            "normal" => "Обычный",
            "hazardous" => "Опасный",
            "perishable" => "Скоропортящийся",
            "oversized" => "Крупногабаритный",
            null or "" => "Не указан",
            _ => cargoType
        };

    private static string FormatDateTime(DateTime value) => value.ToString("dd.MM.yyyy HH:mm", RuCulture);

    private static string BuildContactBlock(string title, string address, string? contactName, string? contactPhone)
    {
        string person = string.IsNullOrWhiteSpace(contactName) ? "Контакт не указан" : contactName;
        string phone = string.IsNullOrWhiteSpace(contactPhone) ? "Телефон не указан" : contactPhone;
        return $"{title}: {address}\n{person}\n{phone}";
    }

    private string BuildCargoMetrics()
    {
        string weight = Order.Cargo is null ? "Вес не указан" : $"{Order.Cargo.WeightKg:0.##} кг";
        string volume = Order.Cargo?.VolumeM3.HasValue == true ? $"{Order.Cargo.VolumeM3.Value:0.##} м3" : "Объём не указан";
        return $"{weight} • {volume}";
    }

    private string BuildScheduleLabel()
    {
        string plannedPickup = Order.PlannedPickupAt.HasValue ? FormatDateTime(Order.PlannedPickupAt.Value) : "Не запланировано";
        string desiredDelivery = Order.DesiredDeliveryAt.HasValue ? FormatDateTime(Order.DesiredDeliveryAt.Value) : "Не указано";
        return $"Погрузка: {plannedPickup}\nЖелаемая доставка: {desiredDelivery}";
    }

    private static string BuildTimelineTitle(OrderStatusHistory item)
    {
        string newStatus = GetStatusName(item.NewStatus);
        if (string.IsNullOrWhiteSpace(item.OldStatus))
        {
            return newStatus;
        }

        return $"{GetStatusName(item.OldStatus!)} -> {newStatus}";
    }

    private string GetStatusChangeComment(string nextStatus) =>
        nextStatus switch
        {
            Order.OrderStatuses.Accepted => "Заказ принят из карточки заказа.",
            Order.OrderStatuses.Loading => "Погрузка запущена из карточки заказа.",
            Order.OrderStatuses.InTransit => "Отправка в путь отмечена из карточки заказа.",
            Order.OrderStatuses.Delivered => "Доставка завершена из карточки заказа.",
            Order.OrderStatuses.Received => "Получение подтверждено из карточки заказа.",
            _ => "Статус изменён из карточки заказа."
        };
}
