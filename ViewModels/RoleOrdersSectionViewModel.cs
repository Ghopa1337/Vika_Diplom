using System.Collections.ObjectModel;
using System.Globalization;
using CargoTransport.Desktop.Models;
using CargoTransport.Desktop.Services;

namespace CargoTransport.Desktop.ViewModels;

public sealed class RoleOrderRowViewModel
{
    public required Order Order { get; init; }
    public required uint Id { get; init; }
    public required string OrderNumber { get; init; }
    public required string Cargo { get; init; }
    public required string CounterpartyTitle { get; init; }
    public required string CounterpartyValue { get; init; }
    public required string Vehicle { get; init; }
    public required string StatusCode { get; init; }
    public required string StatusName { get; init; }
    public required string Route { get; init; }
    public required string DeliveryDate { get; init; }
    public required string Cost { get; init; }
}

public sealed class RoleOrdersSectionViewModel : ViewModelBase
{
    private readonly IRoleOrderWorkspaceService _roleOrderWorkspaceService;
    private readonly RoleOrderCabinetMode _mode;
    private readonly Action<Order> _openOrderDetails;
    private readonly Func<Task> _onOrdersStateChanged;
    private readonly List<Order> _allOrders = [];

    private string _filterSearch = string.Empty;
    private string _filterStatusCode = string.Empty;
    private RoleOrderRowViewModel? _selectedOrder;
    private string _statusMessage = string.Empty;
    private bool _isBusy;

    public RoleOrdersSectionViewModel(
        IRoleOrderWorkspaceService roleOrderWorkspaceService,
        RoleOrderCabinetMode mode,
        Action<Order> openOrderDetails,
        Func<Task> onOrdersStateChanged)
    {
        _roleOrderWorkspaceService = roleOrderWorkspaceService;
        _mode = mode;
        _openOrderDetails = openOrderDetails;
        _onOrdersStateChanged = onOrdersStateChanged;

        ApplyFilterCommand = new RelayCommand(ApplyFilters, () => !IsBusy);
        ClearFilterCommand = new RelayCommand(ClearFilters, () => !IsBusy);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
        OpenDetailsCommand = new RelayCommand(OpenDetails, () => SelectedOrder is not null);
        AcceptCommand = new AsyncRelayCommand(() => ExecuteActionAsync(RoleOrderAction.Accept), () => CanAccept);
        RefuseCommand = new AsyncRelayCommand(() => ExecuteActionAsync(RoleOrderAction.Refuse), () => CanRefuse);
        StartLoadingCommand = new AsyncRelayCommand(() => ExecuteActionAsync(RoleOrderAction.StartLoading), () => CanStartLoading);
        MarkDeliveredCommand = new AsyncRelayCommand(() => ExecuteActionAsync(RoleOrderAction.MarkDelivered), () => CanMarkDelivered);
        ConfirmReceiptCommand = new AsyncRelayCommand(() => ExecuteActionAsync(RoleOrderAction.ConfirmReceipt), () => CanConfirmReceipt);
    }

    public string HeaderTitle => _mode == RoleOrderCabinetMode.Receiver ? "Мои заказы" : "Мои рейсы";
    public string HeaderSubtitle => _mode == RoleOrderCabinetMode.Receiver
        ? "Личный список заказов текущего получателя с подтверждением получения."
        : "Назначенные перевозки текущего водителя со сменой рабочих статусов.";
    public string CounterpartyColumnTitle => _mode == RoleOrderCabinetMode.Receiver ? "Водитель" : "Получатель";
    public string ActionPanelTitle => _mode == RoleOrderCabinetMode.Receiver ? "Действия получателя" : "Действия водителя";
    public bool IsReceiverMode => _mode == RoleOrderCabinetMode.Receiver;
    public bool IsDriverMode => _mode == RoleOrderCabinetMode.Driver;
    public bool IsBusy
    {
        get => _isBusy;
        private set => Set(ref _isBusy, value);
    }

    public string FilterSearch
    {
        get => _filterSearch;
        set => Set(ref _filterSearch, value);
    }

    public string FilterStatusCode
    {
        get => _filterStatusCode;
        set => Set(ref _filterStatusCode, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => Set(ref _statusMessage, value);
    }

    public ObservableCollection<AdminStatTileViewModel> Metrics { get; } = [];
    public ObservableCollection<RoleOrderRowViewModel> Orders { get; } = [];
    public ObservableCollection<AdminChoiceViewModel> StatusFilterOptions { get; } =
    [
        new(string.Empty, "Все статусы"),
        new("created", "Создан"),
        new("assigned", "Назначен"),
        new("accepted", "Принят"),
        new("loading", "Погрузка"),
        new("in_transit", "В пути"),
        new("delivered", "Доставлен"),
        new("received", "Получен"),
        new("cancelled", "Отменен")
    ];

    public RoleOrderRowViewModel? SelectedOrder
    {
        get => _selectedOrder;
        set => Set(ref _selectedOrder, value);
    }

    public RelayCommand ApplyFilterCommand { get; }
    public RelayCommand ClearFilterCommand { get; }
    public AsyncRelayCommand RefreshCommand { get; }
    public RelayCommand OpenDetailsCommand { get; }
    public AsyncRelayCommand AcceptCommand { get; }
    public AsyncRelayCommand RefuseCommand { get; }
    public AsyncRelayCommand StartLoadingCommand { get; }
    public AsyncRelayCommand MarkDeliveredCommand { get; }
    public AsyncRelayCommand ConfirmReceiptCommand { get; }

    public bool CanAccept => IsDriverMode && SelectedOrder?.StatusCode == Order.OrderStatuses.Assigned;
    public bool CanRefuse => IsDriverMode && SelectedOrder?.StatusCode == Order.OrderStatuses.Assigned;
    public bool CanStartLoading => IsDriverMode && SelectedOrder?.StatusCode == Order.OrderStatuses.Accepted;
    public bool CanMarkDelivered => IsDriverMode && SelectedOrder?.StatusCode is Order.OrderStatuses.Accepted or Order.OrderStatuses.Loading or Order.OrderStatuses.InTransit;
    public bool CanConfirmReceipt => IsReceiverMode && SelectedOrder?.StatusCode == Order.OrderStatuses.Delivered;

    public int TotalOrdersCount => _allOrders.Count;
    public int ActiveOrdersCount => _allOrders.Count(x => x.Status is not (Order.OrderStatuses.Received or Order.OrderStatuses.Cancelled));
    public int AwaitingActionCount => _mode == RoleOrderCabinetMode.Receiver
        ? _allOrders.Count(x => x.Status == Order.OrderStatuses.Delivered)
        : _allOrders.Count(x => x.Status == Order.OrderStatuses.Assigned);

    protected override void OnPropertyChanged(string? propertyName)
    {
        base.OnPropertyChanged(propertyName);

        switch (propertyName)
        {
            case nameof(SelectedOrder):
                OpenDetailsCommand.RaiseCanExecuteChanged();
                RaiseActionStates();
                OnPropertyChanged(nameof(SelectedOrderSummary));
                OnPropertyChanged(nameof(SelectedOrderStatusLabel));
                OnPropertyChanged(nameof(SelectedOrderCounterpartyLabel));
                OnPropertyChanged(nameof(SelectedOrderCounterpartyValue));
                OnPropertyChanged(nameof(SelectedOrderVehicleLabel));
                OnPropertyChanged(nameof(SelectedOrderVehicleValue));
                OnPropertyChanged(nameof(SelectedOrderPickupLabel));
                OnPropertyChanged(nameof(SelectedOrderPickupValue));
                OnPropertyChanged(nameof(SelectedOrderDeliveryLabel));
                OnPropertyChanged(nameof(SelectedOrderDeliveryValue));
                OnPropertyChanged(nameof(SelectedOrderCostValue));
                break;
            case nameof(IsBusy):
                ApplyFilterCommand.RaiseCanExecuteChanged();
                ClearFilterCommand.RaiseCanExecuteChanged();
                RefreshCommand.RaiseCanExecuteChanged();
                RaiseActionStates();
                break;
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsBusy = true;
            IReadOnlyList<Order> orders = await _roleOrderWorkspaceService.GetOrdersForCurrentUserAsync(_mode, cancellationToken);
            _allOrders.Clear();
            _allOrders.AddRange(orders);

            ApplyMetrics();
            ApplyFilters();
            StatusMessage = _allOrders.Count == 0
                ? (_mode == RoleOrderCabinetMode.Receiver ? "У текущего получателя пока нет заказов." : "У текущего водителя пока нет назначенных рейсов.")
                : string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public IEnumerable<AdminStatTileViewModel> GetDashboardMetrics()
    {
        if (_mode == RoleOrderCabinetMode.Receiver)
        {
            return
            [
                new AdminStatTileViewModel("Всего заказов", TotalOrdersCount.ToString(CultureInfo.CurrentCulture), "Личный список доставок текущего получателя."),
                new AdminStatTileViewModel("Активные", ActiveOrdersCount.ToString(CultureInfo.CurrentCulture), "Заказы, которые еще не завершены или не отменены."),
                new AdminStatTileViewModel("Ждут подтверждения", AwaitingActionCount.ToString(CultureInfo.CurrentCulture), "Доставленные заказы, которые нужно подтвердить.")
            ];
        }

        return
        [
            new AdminStatTileViewModel("Всего рейсов", TotalOrdersCount.ToString(CultureInfo.CurrentCulture), "Все заказы, назначенные текущему водителю."),
            new AdminStatTileViewModel("Активные", ActiveOrdersCount.ToString(CultureInfo.CurrentCulture), "Рейсы, которые еще не завершены."),
            new AdminStatTileViewModel("Требуют реакции", AwaitingActionCount.ToString(CultureInfo.CurrentCulture), "Назначенные рейсы, которые ожидают принятия.")
        ];
    }

    public IEnumerable<AdminQuickActionCardViewModel> GetDashboardQuickActions()
    {
        if (_mode == RoleOrderCabinetMode.Receiver)
        {
            return
            [
                new AdminQuickActionCardViewModel("Открыть мои заказы", "Рабочий список заказов получателя уже подключен к текущему пользователю.", "Раздел Мои заказы"),
                new AdminQuickActionCardViewModel("Подтвердить получение", "После статуса Доставлен можно завершить заказ из личного кабинета.", "Действие доступно"),
                new AdminQuickActionCardViewModel("Проверить детали доставки", "По выбранному заказу можно открыть карточку и посмотреть маршрут с историей.", "Карточка заказа")
            ];
        }

        return
        [
            new AdminQuickActionCardViewModel("Открыть мои рейсы", "Личный список рейсов уже подключен к заказам текущего водителя.", "Раздел Мои рейсы"),
            new AdminQuickActionCardViewModel("Принять или отказаться", "По статусу Назначен доступны действия принятия или отказа от рейса.", "Действие доступно"),
            new AdminQuickActionCardViewModel("Обновить рабочий статус", "После принятия можно начать загрузку и затем отметить заказ как доставленный.", "Действие доступно")
        ];
    }

    public string SelectedOrderSummary => SelectedOrder is null
        ? "Выберите заказ в таблице, чтобы увидеть маршрут, контакты и доступные действия."
        : $"{SelectedOrder.OrderNumber} • {SelectedOrder.Cargo}";

    public string SelectedOrderStatusLabel => SelectedOrder?.StatusName ?? "Не выбран";
    public string SelectedOrderCounterpartyLabel => CounterpartyColumnTitle;
    public string SelectedOrderCounterpartyValue => SelectedOrder?.CounterpartyValue ?? "Не выбран";
    public string SelectedOrderVehicleLabel => "Транспорт";
    public string SelectedOrderVehicleValue => SelectedOrder?.Vehicle ?? "Не назначен";
    public string SelectedOrderPickupLabel => "Погрузка";
    public string SelectedOrderPickupValue => SelectedOrder?.Order.PickupAddress ?? "Не выбрано";
    public string SelectedOrderDeliveryLabel => "Доставка";
    public string SelectedOrderDeliveryValue => SelectedOrder?.Order.DeliveryAddress ?? "Не выбрано";
    public string SelectedOrderCostValue => SelectedOrder?.Cost ?? "Не указана";

    private async Task RefreshAsync() => await LoadAsync();

    private void ApplyMetrics()
    {
        AdminCollectionHelper.ReplaceWith(Metrics, GetDashboardMetrics());
        OnPropertyChanged(nameof(TotalOrdersCount));
        OnPropertyChanged(nameof(ActiveOrdersCount));
        OnPropertyChanged(nameof(AwaitingActionCount));
    }

    private void ApplyFilters()
    {
        IEnumerable<Order> filtered = _allOrders;

        if (!string.IsNullOrWhiteSpace(FilterSearch))
        {
            string search = FilterSearch.Trim();
            filtered = filtered.Where(x =>
                Contains(x.OrderNumber, search)
                || Contains(x.Cargo?.Name, search)
                || Contains(x.PickupAddress, search)
                || Contains(x.DeliveryAddress, search)
                || Contains(x.Driver?.User?.FullName, search)
                || Contains(x.ReceiverUser?.CompanyName, search)
                || Contains(x.ReceiverUser?.FullName, search)
                || Contains(x.Vehicle?.LicensePlate, search));
        }

        if (!string.IsNullOrWhiteSpace(FilterStatusCode))
        {
            filtered = filtered.Where(x => string.Equals(x.Status, FilterStatusCode, StringComparison.OrdinalIgnoreCase));
        }

        List<RoleOrderRowViewModel> rows = filtered
            .OrderByDescending(x => x.CreatedAt)
            .Select(MapOrder)
            .ToList();

        AdminCollectionHelper.ReplaceWith(Orders, rows);

        if (SelectedOrder is not null)
        {
            SelectedOrder = rows.FirstOrDefault(x => x.Id == SelectedOrder.Id);
            return;
        }

        SelectedOrder = rows.FirstOrDefault();
    }

    private void ClearFilters()
    {
        FilterSearch = string.Empty;
        FilterStatusCode = string.Empty;
        ApplyFilters();
    }

    private void OpenDetails()
    {
        if (SelectedOrder is null)
        {
            return;
        }

        _openOrderDetails(SelectedOrder.Order);
    }

    private async Task ExecuteActionAsync(RoleOrderAction action)
    {
        if (SelectedOrder is null)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await _roleOrderWorkspaceService.ExecuteActionAsync(_mode, SelectedOrder.Id, action);
            await LoadAsync();
            StatusMessage = action switch
            {
                RoleOrderAction.Accept => "Заказ принят водителем.",
                RoleOrderAction.Refuse => "Водитель отказался от заказа. Он возвращен на повторное назначение.",
                RoleOrderAction.StartLoading => "Для заказа зафиксировано начало загрузки.",
                RoleOrderAction.MarkDelivered => "Заказ отмечен как доставленный.",
                RoleOrderAction.ConfirmReceipt => "Получение заказа подтверждено.",
                _ => "Статус заказа обновлен."
            };

            await _onOrdersStateChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка операции: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private RoleOrderRowViewModel MapOrder(Order order) =>
        new()
        {
            Order = order,
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Cargo = order.Cargo?.Name ?? "Груз не указан",
            CounterpartyTitle = CounterpartyColumnTitle,
            CounterpartyValue = _mode == RoleOrderCabinetMode.Receiver
                ? order.Driver?.User?.FullName ?? "Не назначен"
                : order.ReceiverUser?.CompanyName ?? order.ReceiverUser?.FullName ?? "Не указан",
            Vehicle = order.Vehicle?.LicensePlate ?? "Не назначен",
            StatusCode = order.Status,
            StatusName = GetStatusName(order.Status),
            Route = $"{order.PickupAddress} -> {order.DeliveryAddress}",
            DeliveryDate = order.DesiredDeliveryAt?.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("ru-RU")) ?? "Не назначена",
            Cost = order.TotalCost?.ToString("0.## ₽", CultureInfo.GetCultureInfo("ru-RU")) ?? "Не указана"
        };

    private void RaiseActionStates()
    {
        AcceptCommand.RaiseCanExecuteChanged();
        RefuseCommand.RaiseCanExecuteChanged();
        StartLoadingCommand.RaiseCanExecuteChanged();
        MarkDeliveredCommand.RaiseCanExecuteChanged();
        ConfirmReceiptCommand.RaiseCanExecuteChanged();
    }

    private static bool Contains(string? source, string search) =>
        !string.IsNullOrWhiteSpace(source) && source.Contains(search, StringComparison.OrdinalIgnoreCase);

    private static string GetStatusName(string statusCode) =>
        statusCode switch
        {
            "created" => "Создан",
            "assigned" => "Назначен",
            "accepted" => "Принят",
            "loading" => "Погрузка",
            "in_transit" => "В пути",
            "delivered" => "Доставлен",
            "received" => "Получен",
            "cancelled" => "Отменен",
            _ => statusCode
        };
}
