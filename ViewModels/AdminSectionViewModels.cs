using CargoTransport.Desktop.Models;
using CargoTransport.Desktop.Services;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using CargoTransport.Desktop;

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

public sealed class AdminLookupItemViewModel
{
    public AdminLookupItemViewModel(uint id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }

    public uint Id { get; }
    public string DisplayName { get; }
}

public sealed class AdminChoiceViewModel
{
    public AdminChoiceViewModel(string code, string name)
    {
        Code = code;
        Name = name;
    }

    public string Code { get; }
    public string Name { get; }
}

public sealed class AdminUserRowViewModel
{
    public required uint Id { get; init; }
    public required string Username { get; init; }
    public required string FullName { get; init; }
    public required string Role { get; init; }
    public required string Status { get; init; }
    public required string Phone { get; init; }
    public required string LastLogin { get; init; }
    public required uint RoleId { get; init; }
    public string? Email { get; init; }
    public string? PhoneRaw { get; init; }
    public string? CompanyName { get; init; }
    public bool IsActive { get; init; }
    public bool IsBlocked { get; init; }
    public bool MustChangePassword { get; init; }
}

public sealed class AdminOrderRowViewModel
{
    public required uint Id { get; init; }
    public required string OrderNumber { get; init; }
    public required string Receiver { get; init; }
    public required string Driver { get; init; }
    public required string Vehicle { get; init; }
    public required string Status { get; init; }
    public required string DeliveryDate { get; init; }
    public required uint ReceiverUserId { get; init; }
    public required uint CargoId { get; init; }
    public uint? DriverId { get; init; }
    public uint? VehicleId { get; init; }
    public required string PickupAddress { get; init; }
    public required string DeliveryAddress { get; init; }
    public string? PickupContactName { get; init; }
    public string? PickupContactPhone { get; init; }
    public string? DeliveryContactName { get; init; }
    public string? DeliveryContactPhone { get; init; }
    public decimal? DistanceKm { get; init; }
    public decimal? TotalCost { get; init; }
    public required string StatusCode { get; init; }
    public DateTime? PlannedPickupAt { get; init; }
    public DateTime? DesiredDeliveryAt { get; init; }
    public string? CancellationReason { get; init; }
    public string? Comment { get; init; }
}

public sealed class AdminDriverRowViewModel
{
    public required uint Id { get; init; }
    public required string FullName { get; init; }
    public required string Status { get; init; }
    public required string LicenseNumber { get; init; }
    public required string Experience { get; init; }
    public required string Phone { get; init; }
    public required string CurrentOrder { get; init; }
}

public sealed class AdminVehicleRowViewModel
{
    public required uint Id { get; init; }
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
            new("Создать пользователя", "Быстрый старт для регистрации нового диспетчера, получателя или водителя.", "Раздел Пользователи"),
            new("Открыть заказы", "Раздел с фильтрами, назначением водителя и транспорта, сменой статуса.", "Раздел Заказы"),
            new("Проверить парк ТС", "Контроль доступности транспорта и закрепления за водителями.", "Раздел Транспорт")
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

public abstract class AdminEditableSectionViewModel : ViewModelBase
{
    private readonly Func<CancellationToken, Task> _refreshPanelAsync;
    private bool _isBusy;
    private string? _statusMessage;

    protected AdminEditableSectionViewModel(
        IAdminCrudService adminCrudService,
        Func<CancellationToken, Task> refreshPanelAsync)
    {
        AdminCrudService = adminCrudService;
        _refreshPanelAsync = refreshPanelAsync;
    }

    protected IAdminCrudService AdminCrudService { get; }

    public bool IsBusy
    {
        get => _isBusy;
        set => Set(ref _isBusy, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        set => Set(ref _statusMessage, value);
    }

    public abstract Task LoadLookupsAsync(CancellationToken cancellationToken = default);

    public virtual SieveModel? BuildSieveModel() => null;

    protected override void OnPropertyChanged(string? propertyName)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(IsBusy))
        {
            RaiseCommandStates();
        }
    }

    protected async Task<bool> ExecuteCrudAsync(Func<Task> action, string successMessage)
    {
        IsBusy = true;

        try
        {
            await action();
            StatusMessage = successMessage;
            await _refreshPanelAsync(CancellationToken.None);
            return true;
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
            return false;
        }
        catch (DbUpdateException)
        {
            StatusMessage = "База не дала выполнить операцию. Скорее всего, запись уже связана с заказами или другими таблицами.";
            return false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
            return false;
        }
        finally
        {
            IsBusy = false;
            RaiseCommandStates();
        }
    }

    protected virtual void RaiseCommandStates()
    {
    }

    protected Task RefreshPanelAsync() => _refreshPanelAsync(CancellationToken.None);
}

public sealed class AdminUsersSectionViewModel : AdminEditableSectionViewModel
{
    private readonly RelayCommand _beginCreateCommand;
    private readonly AsyncRelayCommand _saveCommand;
    private readonly AsyncRelayCommand _deleteCommand;
    private readonly AsyncRelayCommand _applyFilterCommand;
    private readonly RelayCommand _resetCommand;
    private readonly RelayCommand _clearFilterCommand;
    private AdminUserRowViewModel? _selectedUser;
    private uint? _editingUserId;
    private uint? _deleteArmedUserId;
    private string _filterSearch = string.Empty;
    private uint? _filterRoleId;
    private string _filterStatusCode = string.Empty;
    private string _selectedSort = "role,username";
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _fullName = string.Empty;
    private uint? _selectedRoleId;
    private string _email = string.Empty;
    private string _phone = string.Empty;
    private string _companyName = string.Empty;
    private bool _isActive = true;
    private bool _isBlocked;
    private bool _mustChangePassword;

    public AdminUsersSectionViewModel(
        IAdminCrudService adminCrudService,
        Func<CancellationToken, Task> refreshPanelAsync)
        : base(adminCrudService, refreshPanelAsync)
    {
        _beginCreateCommand = new RelayCommand(BeginCreate, () => !IsBusy);
        _saveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        _deleteCommand = new AsyncRelayCommand(DeleteAsync, () => !IsBusy && SelectedUser is not null);
        _applyFilterCommand = new AsyncRelayCommand(RefreshPanelAsync, () => !IsBusy);
        _resetCommand = new RelayCommand(ResetForm, () => !IsBusy);
        _clearFilterCommand = new RelayCommand(ClearFilters, () => !IsBusy);
    }

    public ObservableCollection<AdminStatTileViewModel> Stats { get; } = [];
    public ObservableCollection<AdminUserRowViewModel> Users { get; } = [];
    public ObservableCollection<AdminLookupItemViewModel> RoleOptions { get; } = [];
    public ObservableCollection<AdminLookupItemViewModel> RoleFilterOptions { get; } = [];
    public ObservableCollection<AdminChoiceViewModel> StatusFilterOptions { get; } =
    [
        new(string.Empty, "Все статусы"),
        new("active", "Активные"),
        new("blocked", "Заблокированные"),
        new("inactive", "Неактивные")
    ];

    public ObservableCollection<AdminChoiceViewModel> SortOptions { get; } =
    [
        new("role,username", "Роль и логин"),
        new("username", "Логин"),
        new("fullName", "ФИО / организация"),
        new("-createdAt", "Новые сверху"),
        new("-lastLoginAt", "Последний вход")
    ];

    public ICommand BeginCreateCommand => _beginCreateCommand;
    public ICommand SaveCommand => _saveCommand;
    public ICommand DeleteCommand => _deleteCommand;
    public ICommand ResetCommand => _resetCommand;
    public ICommand ApplyFilterCommand => _applyFilterCommand;
    public ICommand ClearFilterCommand => _clearFilterCommand;

    public AdminUserRowViewModel? SelectedUser
    {
        get => _selectedUser;
        set => Set(ref _selectedUser, value);
    }

    public uint? EditingUserId
    {
        get => _editingUserId;
        set => Set(ref _editingUserId, value);
    }

    public string FilterSearch
    {
        get => _filterSearch;
        set => Set(ref _filterSearch, value);
    }

    public uint? FilterRoleId
    {
        get => _filterRoleId;
        set => Set(ref _filterRoleId, value);
    }

    public string FilterStatusCode
    {
        get => _filterStatusCode;
        set => Set(ref _filterStatusCode, value);
    }

    public string SelectedSort
    {
        get => _selectedSort;
        set => Set(ref _selectedSort, value);
    }

    public string Username
    {
        get => _username;
        set => Set(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => Set(ref _password, value);
    }

    public string FullName
    {
        get => _fullName;
        set => Set(ref _fullName, value);
    }

    public uint? SelectedRoleId
    {
        get => _selectedRoleId;
        set => Set(ref _selectedRoleId, value);
    }

    public string Email
    {
        get => _email;
        set => Set(ref _email, InputValidationHelper.NormalizeEmailInput(value));
    }

    public string Phone
    {
        get => _phone;
        set => Set(ref _phone, InputValidationHelper.KeepDigitsOnly(value));
    }

    public string CompanyName
    {
        get => _companyName;
        set => Set(ref _companyName, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => Set(ref _isActive, value);
    }

    public bool IsBlocked
    {
        get => _isBlocked;
        set => Set(ref _isBlocked, value);
    }

    public bool MustChangePassword
    {
        get => _mustChangePassword;
        set => Set(ref _mustChangePassword, value);
    }

    public string FormTitle => EditingUserId.HasValue ? "Редактирование пользователя" : "Новый пользователь";
    public string PasswordHint => EditingUserId.HasValue ? "Оставь пустым, если пароль не меняем" : "Пароль обязателен";

    public override async Task LoadLookupsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AdminLookupItemData> roles = await AdminCrudService.GetRoleOptionsAsync(cancellationToken);
        AdminCollectionHelper.ReplaceWith(RoleOptions, roles.Select(x => new AdminLookupItemViewModel(x.Id, x.DisplayName)));
        AdminCollectionHelper.ReplaceWith(RoleFilterOptions, AdminLookupHelper.WithEmpty(roles, "Все роли"));

        FilterRoleId = AdminLookupSelectionHelper.NormalizeOptionalSelection(FilterRoleId, RoleFilterOptions);
        SelectedRoleId = AdminLookupSelectionHelper.NormalizeRequiredSelection(SelectedRoleId, RoleOptions);
    }

    public void ApplyData(AdminUsersData data)
    {
        AdminCollectionHelper.ReplaceWith(
            Stats,
            data.Stats.Select(x => new AdminStatTileViewModel(x.Title, x.Value, x.Hint)));

        AdminCollectionHelper.ReplaceWith(
            Users,
            data.Users.Select(x => new AdminUserRowViewModel
            {
                Id = x.Id,
                Username = x.Username,
                FullName = x.FullName,
                Role = x.Role,
                Status = x.Status,
                Phone = x.Phone,
                LastLogin = x.LastLogin,
                RoleId = x.RoleId,
                Email = x.Email,
                PhoneRaw = x.PhoneRaw,
                CompanyName = x.CompanyName,
                IsActive = x.IsActive,
                IsBlocked = x.IsBlocked,
                MustChangePassword = x.MustChangePassword
            }));
    }

    public override SieveModel BuildSieveModel()
    {
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(FilterSearch))
        {
            string value = AdminSieveHelper.NormalizeValue(FilterSearch);
            filters.Add($"(Username|FullName|CompanyName|Email|Phone)@={value}");
        }

        if (FilterRoleId is > 0)
        {
            filters.Add($"RoleId=={FilterRoleId.Value}");
        }

        switch (FilterStatusCode)
        {
            case "active":
                filters.Add("IsActive==true");
                filters.Add("IsBlocked==false");
                break;
            case "blocked":
                filters.Add("IsBlocked==true");
                break;
            case "inactive":
                filters.Add("IsActive==false");
                break;
        }

        return new SieveModel
        {
            Filters = AdminSieveHelper.JoinFilters(filters),
            Sorts = AdminSieveHelper.NormalizeSort(SelectedSort)
        };
    }

    protected override void OnPropertyChanged(string? propertyName)
    {
        base.OnPropertyChanged(propertyName);

        switch (propertyName)
        {
            case nameof(SelectedUser):
                _deleteArmedUserId = null;
                FillForm(SelectedUser);
                RaiseCommandStates();
                break;
            case nameof(EditingUserId):
                OnPropertyChanged(nameof(FormTitle));
                OnPropertyChanged(nameof(PasswordHint));
                break;
            case nameof(Username):
            case nameof(FullName):
            case nameof(SelectedRoleId):
            case nameof(Email):
            case nameof(Phone):
            case nameof(Password):
                _saveCommand.RaiseCanExecuteChanged();
                break;
        }
    }

    protected override void RaiseCommandStates()
    {
        _beginCreateCommand.RaiseCanExecuteChanged();
        _saveCommand.RaiseCanExecuteChanged();
        _deleteCommand.RaiseCanExecuteChanged();
        _applyFilterCommand.RaiseCanExecuteChanged();
        _resetCommand.RaiseCanExecuteChanged();
        _clearFilterCommand.RaiseCanExecuteChanged();
    }

    private void ClearFilters()
    {
        FilterSearch = string.Empty;
        FilterRoleId = AdminLookupSelectionHelper.NormalizeOptionalSelection(0, RoleFilterOptions);
        FilterStatusCode = string.Empty;
        SelectedSort = "role,username";
        _ = RefreshPanelAsync();
    }

    private void BeginCreate()
    {
        SelectedUser = null;
        ResetForm();
        StatusMessage = "Заполни форму и нажми Сохранить.";
    }

    private void ResetForm()
    {
        EditingUserId = null;
        Username = string.Empty;
        Password = string.Empty;
        FullName = string.Empty;
        SelectedRoleId = AdminLookupSelectionHelper.NormalizeRequiredSelection(null, RoleOptions);
        Email = string.Empty;
        Phone = string.Empty;
        CompanyName = string.Empty;
        IsActive = true;
        IsBlocked = false;
        MustChangePassword = false;
        _deleteArmedUserId = null;
    }

    private void FillForm(AdminUserRowViewModel? user)
    {
        if (user is null)
        {
            ResetForm();
            return;
        }

        EditingUserId = user.Id;
        Username = user.Username;
        Password = string.Empty;
        FullName = user.FullName;
        SelectedRoleId = user.RoleId;
        Email = user.Email ?? string.Empty;
        Phone = user.PhoneRaw ?? string.Empty;
        CompanyName = user.CompanyName ?? string.Empty;
        IsActive = user.IsActive;
        IsBlocked = user.IsBlocked;
        MustChangePassword = user.MustChangePassword;
    }

    private bool CanSave() =>
        !IsBusy
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(FullName)
        && SelectedRoleId is > 0
        && !string.IsNullOrWhiteSpace(Email)
        && InputValidationHelper.IsValidAsciiEmail(Email)
        && !string.IsNullOrWhiteSpace(Phone)
        && (EditingUserId.HasValue || !string.IsNullOrWhiteSpace(Password));

    private async Task SaveAsync()
    {
        var data = new AdminUserEditData(
            EditingUserId,
            Username,
            Password,
            FullName,
            SelectedRoleId.GetValueOrDefault(),
            Email,
            Phone,
            CompanyName,
            IsActive,
            IsBlocked,
            MustChangePassword);

        if (EditingUserId.HasValue)
        {
            await ExecuteCrudAsync(() => AdminCrudService.UpdateUserAsync(data), "Пользователь обновлен.");
            return;
        }

        if (await ExecuteCrudAsync(() => AdminCrudService.CreateUserAsync(data), "Пользователь создан."))
        {
            ResetForm();
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedUser is null)
        {
            return;
        }

        if (_deleteArmedUserId != SelectedUser.Id)
        {
            _deleteArmedUserId = SelectedUser.Id;
            StatusMessage = $"Подтверждение: нажми Удалить еще раз, чтобы удалить {SelectedUser.Username}.";
            return;
        }

        uint userId = SelectedUser.Id;
        if (await ExecuteCrudAsync(() => AdminCrudService.DeleteUserAsync(userId), "Пользователь удален."))
        {
            ResetForm();
        }
    }

    private async Task OpenDetailsAsync()
    {
        AdminOrderRowViewModel? SelectedOrder = null;
        Action<Order>? _openOrderDetails = null;
        if (SelectedOrder is null || _openOrderDetails is null)
        {
            return;
        }

        Order? order = await AdminCrudService.GetOrderDetailsAsync(SelectedOrder.Id);
        if (order is null)
        {
            StatusMessage = "Карточка заказа не найдена.";
            return;
        }

        _openOrderDetails(order);
    }

}

public sealed class AdminOrdersSectionViewModel : AdminEditableSectionViewModel
{
    private readonly Action<uint?>? _openOrderWizard;
    private readonly Action<Order>? _openOrderDetails;
    private readonly AsyncRelayCommand _saveCommand;
    private readonly AsyncRelayCommand _deleteCommand;
    private readonly AsyncRelayCommand _openDetailsCommand;
    private readonly AsyncRelayCommand _applyFilterCommand;
    private readonly RelayCommand _beginCreateCommand;
    private readonly RelayCommand _resetCommand;
    private readonly RelayCommand _clearFilterCommand;
    private AdminOrderRowViewModel? _selectedOrder;
    private uint? _editingOrderId;
    private uint? _deleteArmedOrderId;
    private string _filterSearch = string.Empty;
    private string _filterStatusCode = string.Empty;
    private uint? _filterReceiverUserId;
    private uint? _filterDriverId;
    private string _selectedSort = "-CreatedAt";
    private string _orderNumber = string.Empty;
    private uint? _selectedReceiverUserId;
    private uint? _selectedCargoId;
    private uint? _selectedDriverId;
    private uint? _selectedVehicleId;
    private string _pickupAddress = string.Empty;
    private string _deliveryAddress = string.Empty;
    private string _pickupContactName = string.Empty;
    private string _pickupContactPhone = string.Empty;
    private string _deliveryContactName = string.Empty;
    private string _deliveryContactPhone = string.Empty;
    private string _distanceKm = string.Empty;
    private string _totalCost = string.Empty;
    private string _selectedStatusCode = "created";
    private DateTime? _plannedPickupAt;
    private DateTime? _desiredDeliveryAt;
    private string _cancellationReason = string.Empty;
    private string _comment = string.Empty;
    private bool _isUpdatingCalculatedCost;

    public AdminOrdersSectionViewModel(
        IAdminCrudService adminCrudService,
        Func<CancellationToken, Task> refreshPanelAsync,
        Action<uint?>? openOrderWizard = null,
        Action<Order>? openOrderDetails = null)
        : base(adminCrudService, refreshPanelAsync)
    {
        _openOrderWizard = openOrderWizard;
        _openOrderDetails = openOrderDetails;
        _beginCreateCommand = new RelayCommand(BeginCreate, () => !IsBusy);
        _saveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        _deleteCommand = new AsyncRelayCommand(DeleteAsync, () => !IsBusy && SelectedOrder is not null);
        _openDetailsCommand = new AsyncRelayCommand(OpenDetailsAsync, () => !IsBusy && SelectedOrder is not null && _openOrderDetails is not null);
        _applyFilterCommand = new AsyncRelayCommand(RefreshPanelAsync, () => !IsBusy);
        _resetCommand = new RelayCommand(ResetSelectedOrClear, () => !IsBusy);
        _clearFilterCommand = new RelayCommand(ClearFilters, () => !IsBusy);
    }

    public ObservableCollection<AdminStatTileViewModel> Stats { get; } = [];
    public ObservableCollection<AdminOrderRowViewModel> Orders { get; } = [];
    public ObservableCollection<AdminLookupItemViewModel> ReceiverOptions { get; } = [];
    public ObservableCollection<AdminLookupItemViewModel> CargoOptions { get; } = [];
    public ObservableCollection<AdminLookupItemViewModel> DriverOptions { get; } = [];
    public ObservableCollection<AdminLookupItemViewModel> VehicleOptions { get; } = [];
    public ObservableCollection<AdminLookupItemViewModel> ReceiverFilterOptions { get; } = [];
    public ObservableCollection<AdminLookupItemViewModel> DriverFilterOptions { get; } = [];
    public ObservableCollection<AdminChoiceViewModel> StatusOptions { get; } =
    [
        new("created", "Создан"),
        new("assigned", "Назначен"),
        new("accepted", "Принят"),
        new("loading", "Погрузка"),
        new("in_transit", "В пути"),
        new("delivered", "Доставлен"),
        new("received", "Получен"),
        new("cancelled", "Отменен")
    ];
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

    public ObservableCollection<AdminChoiceViewModel> SortOptions { get; } =
    [
        new("-CreatedAt", "Новые сверху"),
        new("DesiredDeliveryAt", "Дата доставки"),
        new("Status", "Статус"),
        new("OrderNumber", "Номер заказа"),
        new("receiver", "Получатель")
    ];

    public ICommand BeginCreateCommand => _beginCreateCommand;
    public ICommand SaveCommand => _saveCommand;
    public ICommand DeleteCommand => _deleteCommand;
    public ICommand OpenDetailsCommand => _openDetailsCommand;
    public ICommand ResetCommand => _resetCommand;
    public ICommand ApplyFilterCommand => _applyFilterCommand;
    public ICommand ClearFilterCommand => _clearFilterCommand;

    public AdminOrderRowViewModel? SelectedOrder
    {
        get => _selectedOrder;
        set => Set(ref _selectedOrder, value);
    }

    public uint? EditingOrderId
    {
        get => _editingOrderId;
        set => Set(ref _editingOrderId, value);
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

    public uint? FilterReceiverUserId
    {
        get => _filterReceiverUserId;
        set => Set(ref _filterReceiverUserId, value);
    }

    public uint? FilterDriverId
    {
        get => _filterDriverId;
        set => Set(ref _filterDriverId, value);
    }

    public string SelectedSort
    {
        get => _selectedSort;
        set => Set(ref _selectedSort, value);
    }

    public string OrderNumber
    {
        get => _orderNumber;
        set => Set(ref _orderNumber, value);
    }

    public uint? SelectedReceiverUserId
    {
        get => _selectedReceiverUserId;
        set => Set(ref _selectedReceiverUserId, value);
    }

    public uint? SelectedCargoId
    {
        get => _selectedCargoId;
        set => Set(ref _selectedCargoId, value);
    }

    public uint? SelectedDriverId
    {
        get => _selectedDriverId;
        set => Set(ref _selectedDriverId, value);
    }

    public uint? SelectedVehicleId
    {
        get => _selectedVehicleId;
        set => Set(ref _selectedVehicleId, value);
    }

    public string PickupAddress
    {
        get => _pickupAddress;
        set => Set(ref _pickupAddress, value);
    }

    public string DeliveryAddress
    {
        get => _deliveryAddress;
        set => Set(ref _deliveryAddress, value);
    }

    public string PickupContactName
    {
        get => _pickupContactName;
        set => Set(ref _pickupContactName, value);
    }

    public string PickupContactPhone
    {
        get => _pickupContactPhone;
        set => Set(ref _pickupContactPhone, InputValidationHelper.KeepDigitsOnly(value));
    }

    public string DeliveryContactName
    {
        get => _deliveryContactName;
        set => Set(ref _deliveryContactName, value);
    }

    public string DeliveryContactPhone
    {
        get => _deliveryContactPhone;
        set => Set(ref _deliveryContactPhone, InputValidationHelper.KeepDigitsOnly(value));
    }

    public string DistanceKm
    {
        get => _distanceKm;
        set
        {
            if (Set(ref _distanceKm, value))
            {
                UpdateCalculatedCost();
            }
        }
    }

    public string TotalCost
    {
        get => _totalCost;
        set => Set(ref _totalCost, value);
    }

    public string SelectedStatusCode
    {
        get => _selectedStatusCode;
        set => Set(ref _selectedStatusCode, value);
    }

    public DateTime? PlannedPickupAt
    {
        get => _plannedPickupAt;
        set => Set(ref _plannedPickupAt, value);
    }

    public DateTime? DesiredDeliveryAt
    {
        get => _desiredDeliveryAt;
        set => Set(ref _desiredDeliveryAt, value);
    }

    public string CancellationReason
    {
        get => _cancellationReason;
        set => Set(ref _cancellationReason, value);
    }

    public string Comment
    {
        get => _comment;
        set => Set(ref _comment, value);
    }

    public DateTime MinSelectableDate => DateTime.Today;

    public bool HasSelectedOrder => SelectedOrder is not null;
    public string FormTitle => HasSelectedOrder ? "Карточка заказа" : "Создание заказа";
    public string FormSubtitle => HasSelectedOrder
        ? "Выбранный заказ можно скорректировать или удалить. Для нового заказа используйте пошаговый мастер."
        : "Новые заказы создаются через мастер: получатель, груз, маршрут и назначение по шагам.";

    public override async Task LoadLookupsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AdminLookupItemData> receivers = await AdminCrudService.GetReceiverOptionsAsync(cancellationToken);
        IReadOnlyList<AdminLookupItemData> cargo = await AdminCrudService.GetCargoOptionsAsync(cancellationToken);
        IReadOnlyList<AdminLookupItemData> drivers = await AdminCrudService.GetDriverOptionsAsync(cancellationToken);
        IReadOnlyList<AdminLookupItemData> vehicles = await AdminCrudService.GetVehicleOptionsAsync(cancellationToken);

        AdminCollectionHelper.ReplaceWith(ReceiverOptions, receivers.Select(x => new AdminLookupItemViewModel(x.Id, x.DisplayName)));
        AdminCollectionHelper.ReplaceWith(CargoOptions, cargo.Select(x => new AdminLookupItemViewModel(x.Id, x.DisplayName)));
        AdminCollectionHelper.ReplaceWith(DriverOptions, AdminLookupHelper.WithEmpty(drivers, "Водитель не назначен"));
        AdminCollectionHelper.ReplaceWith(VehicleOptions, AdminLookupHelper.WithEmpty(vehicles, "Транспорт не назначен"));
        AdminCollectionHelper.ReplaceWith(ReceiverFilterOptions, AdminLookupHelper.WithEmpty(receivers, "Все получатели"));
        AdminCollectionHelper.ReplaceWith(DriverFilterOptions, AdminLookupHelper.WithEmpty(drivers, "Все водители"));

        FilterReceiverUserId = AdminLookupSelectionHelper.NormalizeOptionalSelection(FilterReceiverUserId, ReceiverFilterOptions);
        FilterDriverId = AdminLookupSelectionHelper.NormalizeOptionalSelection(FilterDriverId, DriverFilterOptions);
        SelectedReceiverUserId = AdminLookupSelectionHelper.NormalizeRequiredSelection(SelectedReceiverUserId, ReceiverOptions);
        SelectedCargoId = AdminLookupSelectionHelper.NormalizeRequiredSelection(SelectedCargoId, CargoOptions);
        SelectedDriverId = AdminLookupSelectionHelper.NormalizeOptionalSelection(SelectedDriverId, DriverOptions);
        SelectedVehicleId = AdminLookupSelectionHelper.NormalizeOptionalSelection(SelectedVehicleId, VehicleOptions);
    }

    public void ApplyData(AdminOrdersData data)
    {
        AdminCollectionHelper.ReplaceWith(
            Stats,
            data.Stats.Select(x => new AdminStatTileViewModel(x.Title, x.Value, x.Hint)));

        AdminCollectionHelper.ReplaceWith(
            Orders,
            data.Orders.Select(x => new AdminOrderRowViewModel
            {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                Receiver = x.Receiver,
                Driver = x.Driver,
                Vehicle = x.Vehicle,
                Status = x.Status,
                DeliveryDate = x.DeliveryDate,
                ReceiverUserId = x.ReceiverUserId,
                CargoId = x.CargoId,
                DriverId = x.DriverId,
                VehicleId = x.VehicleId,
                PickupAddress = x.PickupAddress,
                DeliveryAddress = x.DeliveryAddress,
                PickupContactName = x.PickupContactName,
                PickupContactPhone = x.PickupContactPhone,
                DeliveryContactName = x.DeliveryContactName,
                DeliveryContactPhone = x.DeliveryContactPhone,
                DistanceKm = x.DistanceKm,
                TotalCost = x.TotalCost,
                StatusCode = x.StatusCode,
                PlannedPickupAt = x.PlannedPickupAt,
                DesiredDeliveryAt = x.DesiredDeliveryAt,
                CancellationReason = x.CancellationReason,
                Comment = x.Comment
            }));
    }

    public override SieveModel BuildSieveModel()
    {
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(FilterSearch))
        {
            string value = AdminSieveHelper.NormalizeValue(FilterSearch);
            filters.Add($"(OrderNumber|PickupAddress|DeliveryAddress|receiver|receiverName|cargoName|driverName|vehiclePlate)@={value}");
        }

        if (!string.IsNullOrWhiteSpace(FilterStatusCode))
        {
            filters.Add($"Status=={AdminSieveHelper.NormalizeValue(FilterStatusCode)}");
        }

        if (FilterReceiverUserId is > 0)
        {
            filters.Add($"ReceiverUserId=={FilterReceiverUserId.Value}");
        }

        if (FilterDriverId is > 0)
        {
            filters.Add($"DriverId=={FilterDriverId.Value}");
        }

        return new SieveModel
        {
            Filters = AdminSieveHelper.JoinFilters(filters),
            Sorts = AdminSieveHelper.NormalizeSort(SelectedSort)
        };
    }

    protected override void OnPropertyChanged(string? propertyName)
    {
        base.OnPropertyChanged(propertyName);

        switch (propertyName)
        {
            case nameof(SelectedOrder):
                _deleteArmedOrderId = null;
                FillForm(SelectedOrder);
                OnPropertyChanged(nameof(HasSelectedOrder));
                OnPropertyChanged(nameof(FormTitle));
                OnPropertyChanged(nameof(FormSubtitle));
                RaiseCommandStates();
                break;
            case nameof(EditingOrderId):
                OnPropertyChanged(nameof(FormTitle));
                break;
            case nameof(SelectedReceiverUserId):
            case nameof(SelectedCargoId):
            case nameof(PickupAddress):
            case nameof(DeliveryAddress):
            case nameof(DistanceKm):
            case nameof(TotalCost):
                _saveCommand.RaiseCanExecuteChanged();
                break;
        }
    }

    protected override void RaiseCommandStates()
    {
        _beginCreateCommand.RaiseCanExecuteChanged();
        _saveCommand.RaiseCanExecuteChanged();
        _deleteCommand.RaiseCanExecuteChanged();
        _openDetailsCommand.RaiseCanExecuteChanged();
        _applyFilterCommand.RaiseCanExecuteChanged();
        _resetCommand.RaiseCanExecuteChanged();
        _clearFilterCommand.RaiseCanExecuteChanged();
    }

    private void ClearFilters()
    {
        FilterSearch = string.Empty;
        FilterStatusCode = string.Empty;
        FilterReceiverUserId = AdminLookupSelectionHelper.NormalizeOptionalSelection(0, ReceiverFilterOptions);
        FilterDriverId = AdminLookupSelectionHelper.NormalizeOptionalSelection(0, DriverFilterOptions);
        SelectedSort = "-CreatedAt";
        _ = RefreshPanelAsync();
    }

    private void BeginCreate()
    {
        SelectedOrder = null;
        ResetForm();

        if (_openOrderWizard is null)
        {
            StatusMessage = "Мастер заказа сейчас недоступен.";
            return;
        }

        StatusMessage = "Открыт пошаговый мастер создания заказа.";
        _openOrderWizard(null);
    }

    private void ResetForm()
    {
        EditingOrderId = null;
        OrderNumber = string.Empty;
        SelectedReceiverUserId = AdminLookupSelectionHelper.NormalizeRequiredSelection(null, ReceiverOptions);
        SelectedCargoId = AdminLookupSelectionHelper.NormalizeRequiredSelection(null, CargoOptions);
        SelectedDriverId = AdminLookupSelectionHelper.NormalizeOptionalSelection(0, DriverOptions);
        SelectedVehicleId = AdminLookupSelectionHelper.NormalizeOptionalSelection(0, VehicleOptions);
        PickupAddress = string.Empty;
        DeliveryAddress = string.Empty;
        PickupContactName = string.Empty;
        PickupContactPhone = string.Empty;
        DeliveryContactName = string.Empty;
        DeliveryContactPhone = string.Empty;
        DistanceKm = string.Empty;
        TotalCost = string.Empty;
        SelectedStatusCode = "created";
        PlannedPickupAt = null;
        DesiredDeliveryAt = null;
        CancellationReason = string.Empty;
        Comment = string.Empty;
        _deleteArmedOrderId = null;
    }

    private void ResetSelectedOrClear()
    {
        if (SelectedOrder is not null)
        {
            FillForm(SelectedOrder);
            StatusMessage = "Карточка заказа восстановлена из выбранной строки.";
            return;
        }

        ResetForm();
        StatusMessage = "Форма очищена. Для нового заказа откройте мастер.";
    }

    private void FillForm(AdminOrderRowViewModel? order)
    {
        if (order is null)
        {
            ResetForm();
            return;
        }

        EditingOrderId = order.Id;
        OrderNumber = order.OrderNumber;
        SelectedReceiverUserId = order.ReceiverUserId;
        SelectedCargoId = order.CargoId;
        SelectedDriverId = order.DriverId ?? 0;
        SelectedVehicleId = order.VehicleId ?? 0;
        PickupAddress = order.PickupAddress;
        DeliveryAddress = order.DeliveryAddress;
        PickupContactName = order.PickupContactName ?? string.Empty;
        PickupContactPhone = order.PickupContactPhone ?? string.Empty;
        DeliveryContactName = order.DeliveryContactName ?? string.Empty;
        DeliveryContactPhone = order.DeliveryContactPhone ?? string.Empty;
        DistanceKm = AdminParsingHelper.FormatDecimal(order.DistanceKm);
        TotalCost = AdminParsingHelper.FormatDecimal(order.TotalCost);
        SelectedStatusCode = order.StatusCode;
        PlannedPickupAt = order.PlannedPickupAt;
        DesiredDeliveryAt = order.DesiredDeliveryAt;
        CancellationReason = order.CancellationReason ?? string.Empty;
        Comment = order.Comment ?? string.Empty;
    }

    private bool CanSave() =>
        !IsBusy
        && EditingOrderId.HasValue
        && SelectedReceiverUserId is > 0
        && SelectedCargoId is > 0
        && !string.IsNullOrWhiteSpace(PickupAddress)
        && !string.IsNullOrWhiteSpace(DeliveryAddress);

    private async Task SaveAsync()
    {
        if (!AdminParsingHelper.TryParseNullableDecimal(DistanceKm, out decimal? distanceKm, out string? distanceError))
        {
            StatusMessage = distanceError;
            return;
        }

        if (!AdminParsingHelper.TryParseNullableDecimal(TotalCost, out decimal? totalCost, out string? costError))
        {
            StatusMessage = costError;
            return;
        }

        var data = new AdminOrderEditData(
            EditingOrderId,
            OrderNumber,
            SelectedReceiverUserId.GetValueOrDefault(),
            SelectedCargoId.GetValueOrDefault(),
            SelectedDriverId is > 0 ? SelectedDriverId : null,
            SelectedVehicleId is > 0 ? SelectedVehicleId : null,
            PickupAddress,
            DeliveryAddress,
            PickupContactName,
            PickupContactPhone,
            DeliveryContactName,
            DeliveryContactPhone,
            distanceKm,
            totalCost,
            SelectedStatusCode,
            PlannedPickupAt,
            DesiredDeliveryAt,
            CancellationReason,
            Comment);

        if (!EditingOrderId.HasValue)
        {
            StatusMessage = "Для создания нового заказа используйте пошаговый мастер.";
            return;
        }

        await ExecuteCrudAsync(() => AdminCrudService.UpdateOrderAsync(data), "Заказ обновлен.");
    }

    private void UpdateCalculatedCost()
    {
        if (_isUpdatingCalculatedCost)
        {
            return;
        }

        _isUpdatingCalculatedCost = true;
        try
        {
            if (!AdminParsingHelper.TryParseNullableDecimal(DistanceKm, out decimal? distanceKm, out _))
            {
                TotalCost = string.Empty;
                return;
            }

            TotalCost = distanceKm.HasValue
                ? AdminParsingHelper.FormatDecimal(InputValidationHelper.CalculateDeliveryCost(distanceKm.Value))
                : string.Empty;
        }
        finally
        {
            _isUpdatingCalculatedCost = false;
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedOrder is null)
        {
            return;
        }

        if (_deleteArmedOrderId != SelectedOrder.Id)
        {
            _deleteArmedOrderId = SelectedOrder.Id;
            StatusMessage = $"Подтверждение: нажми Удалить еще раз, чтобы удалить заказ {SelectedOrder.OrderNumber}.";
            return;
        }

        uint orderId = SelectedOrder.Id;
        if (await ExecuteCrudAsync(() => AdminCrudService.DeleteOrderAsync(orderId), "Заказ удален."))
        {
            ResetForm();
        }
    }

    private async Task OpenDetailsAsync()
    {
        if (SelectedOrder is null || _openOrderDetails is null)
        {
            return;
        }

        Order? order = await AdminCrudService.GetOrderDetailsAsync(SelectedOrder.Id);
        if (order is null)
        {
            StatusMessage = "Карточка заказа не найдена.";
            return;
        }

        _openOrderDetails(order);
    }
}

public sealed class AdminDriversSectionViewModel : AdminEditableSectionViewModel
{
    private readonly AsyncRelayCommand _saveCommand;
    private readonly AsyncRelayCommand _deleteCommand;
    private readonly AsyncRelayCommand _applyFilterCommand;
    private readonly RelayCommand _beginCreateCommand;
    private readonly RelayCommand _resetCommand;
    private readonly RelayCommand _clearFilterCommand;
    private AdminDriverRowViewModel? _selectedDriver;
    private uint? _editingDriverId;
    private uint? _deleteArmedDriverId;
    private string _filterSearch = string.Empty;
    private string _filterStatusCode = string.Empty;
    private string _selectedSort = "driverName";
    private uint? _selectedDriverUserId;
    private string _licenseNumber = string.Empty;
    private string _licenseCategory = "CE";
    private string _experienceYears = "0";
    private string _selectedStatusCode = "available";
    private string _notes = string.Empty;

    public AdminDriversSectionViewModel(
        IAdminCrudService adminCrudService,
        Func<CancellationToken, Task> refreshPanelAsync)
        : base(adminCrudService, refreshPanelAsync)
    {
        _beginCreateCommand = new RelayCommand(BeginCreate, () => !IsBusy);
        _saveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        _deleteCommand = new AsyncRelayCommand(DeleteAsync, () => !IsBusy && SelectedDriver is not null);
        _applyFilterCommand = new AsyncRelayCommand(RefreshPanelAsync, () => !IsBusy);
        _resetCommand = new RelayCommand(ResetForm, () => !IsBusy);
        _clearFilterCommand = new RelayCommand(ClearFilters, () => !IsBusy);
    }

    public ObservableCollection<AdminStatTileViewModel> Stats { get; } = [];
    public ObservableCollection<AdminDriverRowViewModel> Drivers { get; } = [];
    public ObservableCollection<AdminLookupItemViewModel> DriverUserOptions { get; } = [];
    public ObservableCollection<AdminChoiceViewModel> StatusOptions { get; } =
    [
        new("available", "Доступен"),
        new("on_route", "В рейсе"),
        new("rest", "Отдых"),
        new("sick", "Болен")
    ];
    public ObservableCollection<AdminChoiceViewModel> StatusFilterOptions { get; } =
    [
        new(string.Empty, "Все статусы"),
        new("available", "Доступен"),
        new("on_route", "В рейсе"),
        new("rest", "Отдых"),
        new("sick", "Болен")
    ];

    public ObservableCollection<AdminChoiceViewModel> SortOptions { get; } =
    [
        new("driverName", "ФИО"),
        new("Status", "Статус"),
        new("-ExperienceYears", "Стаж"),
        new("LicenseNumber", "Номер ВУ")
    ];

    public ICommand BeginCreateCommand => _beginCreateCommand;
    public ICommand SaveCommand => _saveCommand;
    public ICommand DeleteCommand => _deleteCommand;
    public ICommand ResetCommand => _resetCommand;
    public ICommand ApplyFilterCommand => _applyFilterCommand;
    public ICommand ClearFilterCommand => _clearFilterCommand;

    public AdminDriverRowViewModel? SelectedDriver
    {
        get => _selectedDriver;
        set => Set(ref _selectedDriver, value);
    }

    public uint? EditingDriverId
    {
        get => _editingDriverId;
        set => Set(ref _editingDriverId, value);
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

    public string SelectedSort
    {
        get => _selectedSort;
        set => Set(ref _selectedSort, value);
    }

    public uint? SelectedDriverUserId
    {
        get => _selectedDriverUserId;
        set => Set(ref _selectedDriverUserId, value);
    }

    public string LicenseNumber
    {
        get => _licenseNumber;
        set => Set(ref _licenseNumber, value);
    }

    public string LicenseCategory
    {
        get => _licenseCategory;
        set => Set(ref _licenseCategory, value);
    }

    public string ExperienceYears
    {
        get => _experienceYears;
        set => Set(ref _experienceYears, value);
    }

    public string SelectedStatusCode
    {
        get => _selectedStatusCode;
        set => Set(ref _selectedStatusCode, value);
    }

    public string Notes
    {
        get => _notes;
        set => Set(ref _notes, value);
    }

    public string FormTitle => EditingDriverId.HasValue ? "Редактирование водителя" : "Новый водитель";

    public override async Task LoadLookupsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AdminLookupItemData> users = await AdminCrudService.GetDriverUserOptionsAsync(EditingDriverId, cancellationToken);
        AdminCollectionHelper.ReplaceWith(DriverUserOptions, users.Select(x => new AdminLookupItemViewModel(x.Id, x.DisplayName)));
        SelectedDriverUserId = AdminLookupSelectionHelper.NormalizeRequiredSelection(SelectedDriverUserId, DriverUserOptions);
    }

    public void ApplyData(AdminDriversData data)
    {
        AdminCollectionHelper.ReplaceWith(
            Stats,
            data.Stats.Select(x => new AdminStatTileViewModel(x.Title, x.Value, x.Hint)));

        AdminCollectionHelper.ReplaceWith(
            Drivers,
            data.Drivers.Select(x => new AdminDriverRowViewModel
            {
                Id = x.Id,
                FullName = x.FullName,
                Status = x.Status,
                LicenseNumber = x.LicenseNumber,
                Experience = x.Experience,
                Phone = x.Phone,
                CurrentOrder = x.CurrentOrder
            }));
    }

    public override SieveModel BuildSieveModel()
    {
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(FilterSearch))
        {
            string value = AdminSieveHelper.NormalizeValue(FilterSearch);
            filters.Add($"(driverName|username|LicenseNumber|phone)@={value}");
        }

        if (!string.IsNullOrWhiteSpace(FilterStatusCode))
        {
            filters.Add($"Status=={AdminSieveHelper.NormalizeValue(FilterStatusCode)}");
        }

        return new SieveModel
        {
            Filters = AdminSieveHelper.JoinFilters(filters),
            Sorts = AdminSieveHelper.NormalizeSort(SelectedSort)
        };
    }

    protected override void OnPropertyChanged(string? propertyName)
    {
        base.OnPropertyChanged(propertyName);

        switch (propertyName)
        {
            case nameof(SelectedDriver):
                _deleteArmedDriverId = null;
                _ = LoadSelectedDriverAsync(SelectedDriver);
                RaiseCommandStates();
                break;
            case nameof(EditingDriverId):
                OnPropertyChanged(nameof(FormTitle));
                break;
            case nameof(SelectedDriverUserId):
            case nameof(LicenseNumber):
            case nameof(LicenseCategory):
            case nameof(ExperienceYears):
                _saveCommand.RaiseCanExecuteChanged();
                break;
        }
    }

    protected override void RaiseCommandStates()
    {
        _beginCreateCommand.RaiseCanExecuteChanged();
        _saveCommand.RaiseCanExecuteChanged();
        _deleteCommand.RaiseCanExecuteChanged();
        _applyFilterCommand.RaiseCanExecuteChanged();
        _resetCommand.RaiseCanExecuteChanged();
        _clearFilterCommand.RaiseCanExecuteChanged();
    }

    private void ClearFilters()
    {
        FilterSearch = string.Empty;
        FilterStatusCode = string.Empty;
        SelectedSort = "driverName";
        _ = RefreshPanelAsync();
    }

    private void BeginCreate()
    {
        SelectedDriver = null;
        ResetForm();
        StatusMessage = "Выбери пользователя с ролью водителя и заполни карточку.";
    }

    private void ResetForm()
    {
        EditingDriverId = null;
        SelectedDriverUserId = AdminLookupSelectionHelper.NormalizeRequiredSelection(null, DriverUserOptions);
        LicenseNumber = string.Empty;
        LicenseCategory = "CE";
        ExperienceYears = "0";
        SelectedStatusCode = "available";
        Notes = string.Empty;
        _deleteArmedDriverId = null;
    }

    private async Task LoadSelectedDriverAsync(AdminDriverRowViewModel? driver)
    {
        if (driver is null)
        {
            ResetForm();
            return;
        }

        IsBusy = true;

        try
        {
            AdminDriverEditData? data = await AdminCrudService.GetDriverEditDataAsync(driver.Id);
            if (data is null)
            {
                StatusMessage = "Водитель не найден.";
                return;
            }

            EditingDriverId = data.Id;
            SelectedDriverUserId = data.UserId;
            await LoadLookupsAsync();
            SelectedDriverUserId = data.UserId;
            LicenseNumber = data.LicenseNumber;
            LicenseCategory = data.LicenseCategory;
            ExperienceYears = data.ExperienceYears.ToString(CultureInfo.CurrentCulture);
            SelectedStatusCode = data.Status;
            Notes = data.Notes ?? string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки водителя: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSave() =>
        !IsBusy
        && SelectedDriverUserId is > 0
        && !string.IsNullOrWhiteSpace(LicenseNumber)
        && !string.IsNullOrWhiteSpace(LicenseCategory)
        && ushort.TryParse(ExperienceYears, NumberStyles.Integer, CultureInfo.CurrentCulture, out _);

    private async Task SaveAsync()
    {
        if (!ushort.TryParse(ExperienceYears, NumberStyles.Integer, CultureInfo.CurrentCulture, out ushort parsedExperience))
        {
            StatusMessage = "Стаж должен быть целым числом.";
            return;
        }

        var data = new AdminDriverEditData(
            EditingDriverId,
            SelectedDriverUserId.GetValueOrDefault(),
            LicenseNumber,
            LicenseCategory,
            parsedExperience,
            SelectedStatusCode,
            Notes);

        if (EditingDriverId.HasValue)
        {
            await ExecuteCrudAsync(() => AdminCrudService.UpdateDriverAsync(data), "Водитель обновлен.");
            return;
        }

        if (await ExecuteCrudAsync(() => AdminCrudService.CreateDriverAsync(data), "Водитель создан."))
        {
            ResetForm();
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedDriver is null)
        {
            return;
        }

        if (_deleteArmedDriverId != SelectedDriver.Id)
        {
            _deleteArmedDriverId = SelectedDriver.Id;
            StatusMessage = $"Подтверждение: нажми Удалить еще раз, чтобы удалить водителя {SelectedDriver.FullName}.";
            return;
        }

        uint driverId = SelectedDriver.Id;
        if (await ExecuteCrudAsync(() => AdminCrudService.DeleteDriverAsync(driverId), "Водитель удален."))
        {
            ResetForm();
        }
    }
}

public sealed class AdminVehiclesSectionViewModel : AdminEditableSectionViewModel
{
    private readonly AsyncRelayCommand _saveCommand;
    private readonly AsyncRelayCommand _deleteCommand;
    private readonly AsyncRelayCommand _applyFilterCommand;
    private readonly RelayCommand _beginCreateCommand;
    private readonly RelayCommand _resetCommand;
    private readonly RelayCommand _clearFilterCommand;
    private AdminVehicleRowViewModel? _selectedVehicle;
    private uint? _editingVehicleId;
    private uint? _deleteArmedVehicleId;
    private string _filterSearch = string.Empty;
    private string _filterStatusCode = string.Empty;
    private string _filterBodyTypeCode = string.Empty;
    private string _selectedSort = "LicensePlate";
    private string _licensePlate = string.Empty;
    private string _model = string.Empty;
    private string _capacityKg = string.Empty;
    private string _volumeM3 = string.Empty;
    private string _selectedBodyTypeCode = "van";
    private string _productionYear = string.Empty;
    private string _selectedStatusCode = "available";
    private DateTime? _insuranceExpiry;
    private uint? _selectedCurrentDriverId;
    private string _notes = string.Empty;

    public AdminVehiclesSectionViewModel(
        IAdminCrudService adminCrudService,
        Func<CancellationToken, Task> refreshPanelAsync)
        : base(adminCrudService, refreshPanelAsync)
    {
        _beginCreateCommand = new RelayCommand(BeginCreate, () => !IsBusy);
        _saveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        _deleteCommand = new AsyncRelayCommand(DeleteAsync, () => !IsBusy && SelectedVehicle is not null);
        _applyFilterCommand = new AsyncRelayCommand(RefreshPanelAsync, () => !IsBusy);
        _resetCommand = new RelayCommand(ResetForm, () => !IsBusy);
        _clearFilterCommand = new RelayCommand(ClearFilters, () => !IsBusy);
    }

    public ObservableCollection<AdminStatTileViewModel> Stats { get; } = [];
    public ObservableCollection<AdminVehicleRowViewModel> Vehicles { get; } = [];
    public ObservableCollection<AdminLookupItemViewModel> DriverOptions { get; } = [];
    public ObservableCollection<AdminChoiceViewModel> BodyTypeOptions { get; } =
    [
        new("van", "Фургон"),
        new("curtain", "Тент"),
        new("refrigerator", "Рефрижератор"),
        new("flatbed", "Платформа")
    ];
    public ObservableCollection<AdminChoiceViewModel> BodyTypeFilterOptions { get; } =
    [
        new(string.Empty, "Все типы кузова"),
        new("van", "Фургон"),
        new("curtain", "Тент"),
        new("refrigerator", "Рефрижератор"),
        new("flatbed", "Платформа")
    ];

    public ObservableCollection<AdminChoiceViewModel> StatusOptions { get; } =
    [
        new("available", "Доступен"),
        new("on_route", "В рейсе"),
        new("repair", "Ремонт"),
        new("decommissioned", "Списан")
    ];
    public ObservableCollection<AdminChoiceViewModel> StatusFilterOptions { get; } =
    [
        new(string.Empty, "Все статусы"),
        new("available", "Доступен"),
        new("on_route", "В рейсе"),
        new("repair", "Ремонт"),
        new("decommissioned", "Списан")
    ];

    public ObservableCollection<AdminChoiceViewModel> SortOptions { get; } =
    [
        new("LicensePlate", "Госномер"),
        new("Model", "Модель"),
        new("-CapacityKg", "Грузоподъемность"),
        new("Status", "Статус"),
        new("driverName", "Водитель")
    ];

    public ICommand BeginCreateCommand => _beginCreateCommand;
    public ICommand SaveCommand => _saveCommand;
    public ICommand DeleteCommand => _deleteCommand;
    public ICommand ResetCommand => _resetCommand;
    public ICommand ApplyFilterCommand => _applyFilterCommand;
    public ICommand ClearFilterCommand => _clearFilterCommand;

    public AdminVehicleRowViewModel? SelectedVehicle
    {
        get => _selectedVehicle;
        set => Set(ref _selectedVehicle, value);
    }

    public uint? EditingVehicleId
    {
        get => _editingVehicleId;
        set => Set(ref _editingVehicleId, value);
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

    public string FilterBodyTypeCode
    {
        get => _filterBodyTypeCode;
        set => Set(ref _filterBodyTypeCode, value);
    }

    public string SelectedSort
    {
        get => _selectedSort;
        set => Set(ref _selectedSort, value);
    }

    public string LicensePlate
    {
        get => _licensePlate;
        set => Set(ref _licensePlate, value);
    }

    public string Model
    {
        get => _model;
        set => Set(ref _model, value);
    }

    public string CapacityKg
    {
        get => _capacityKg;
        set => Set(ref _capacityKg, value);
    }

    public string VolumeM3
    {
        get => _volumeM3;
        set => Set(ref _volumeM3, value);
    }

    public string SelectedBodyTypeCode
    {
        get => _selectedBodyTypeCode;
        set => Set(ref _selectedBodyTypeCode, value);
    }

    public string ProductionYear
    {
        get => _productionYear;
        set => Set(ref _productionYear, value);
    }

    public string SelectedStatusCode
    {
        get => _selectedStatusCode;
        set => Set(ref _selectedStatusCode, value);
    }

    public DateTime? InsuranceExpiry
    {
        get => _insuranceExpiry;
        set => Set(ref _insuranceExpiry, value);
    }

    public uint? SelectedCurrentDriverId
    {
        get => _selectedCurrentDriverId;
        set => Set(ref _selectedCurrentDriverId, value);
    }

    public string Notes
    {
        get => _notes;
        set => Set(ref _notes, value);
    }

    public string FormTitle => EditingVehicleId.HasValue ? "Редактирование транспорта" : "Новый транспорт";

    public override async Task LoadLookupsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AdminLookupItemData> drivers = await AdminCrudService.GetVehicleDriverOptionsAsync(EditingVehicleId, cancellationToken);
        AdminCollectionHelper.ReplaceWith(DriverOptions, AdminLookupHelper.WithEmpty(drivers, "Водитель не закреплен"));
        SelectedCurrentDriverId = AdminLookupSelectionHelper.NormalizeOptionalSelection(SelectedCurrentDriverId, DriverOptions);
    }

    public void ApplyData(AdminVehiclesData data)
    {
        AdminCollectionHelper.ReplaceWith(
            Stats,
            data.Stats.Select(x => new AdminStatTileViewModel(x.Title, x.Value, x.Hint)));

        AdminCollectionHelper.ReplaceWith(
            Vehicles,
            data.Vehicles.Select(x => new AdminVehicleRowViewModel
            {
                Id = x.Id,
                LicensePlate = x.LicensePlate,
                Model = x.Model,
                BodyType = x.BodyType,
                Capacity = x.Capacity,
                Status = x.Status,
                AssignedDriver = x.AssignedDriver
            }));
    }

    public override SieveModel BuildSieveModel()
    {
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(FilterSearch))
        {
            string value = AdminSieveHelper.NormalizeValue(FilterSearch);
            filters.Add($"(LicensePlate|Model|BodyType|driverName|driverLicense)@={value}");
        }

        if (!string.IsNullOrWhiteSpace(FilterStatusCode))
        {
            filters.Add($"Status=={AdminSieveHelper.NormalizeValue(FilterStatusCode)}");
        }

        if (!string.IsNullOrWhiteSpace(FilterBodyTypeCode))
        {
            filters.Add($"BodyType=={AdminSieveHelper.NormalizeValue(FilterBodyTypeCode)}");
        }

        return new SieveModel
        {
            Filters = AdminSieveHelper.JoinFilters(filters),
            Sorts = AdminSieveHelper.NormalizeSort(SelectedSort)
        };
    }

    protected override void OnPropertyChanged(string? propertyName)
    {
        base.OnPropertyChanged(propertyName);

        switch (propertyName)
        {
            case nameof(SelectedVehicle):
                _deleteArmedVehicleId = null;
                _ = LoadSelectedVehicleAsync(SelectedVehicle);
                RaiseCommandStates();
                break;
            case nameof(EditingVehicleId):
                OnPropertyChanged(nameof(FormTitle));
                break;
            case nameof(LicensePlate):
            case nameof(Model):
            case nameof(CapacityKg):
            case nameof(VolumeM3):
            case nameof(ProductionYear):
                _saveCommand.RaiseCanExecuteChanged();
                break;
        }
    }

    protected override void RaiseCommandStates()
    {
        _beginCreateCommand.RaiseCanExecuteChanged();
        _saveCommand.RaiseCanExecuteChanged();
        _deleteCommand.RaiseCanExecuteChanged();
        _applyFilterCommand.RaiseCanExecuteChanged();
        _resetCommand.RaiseCanExecuteChanged();
        _clearFilterCommand.RaiseCanExecuteChanged();
    }

    private void ClearFilters()
    {
        FilterSearch = string.Empty;
        FilterStatusCode = string.Empty;
        FilterBodyTypeCode = string.Empty;
        SelectedSort = "LicensePlate";
        _ = RefreshPanelAsync();
    }

    private void BeginCreate()
    {
        SelectedVehicle = null;
        ResetForm();
        StatusMessage = "Заполни данные транспорта и нажми Сохранить.";
    }

    private void ResetForm()
    {
        EditingVehicleId = null;
        LicensePlate = string.Empty;
        Model = string.Empty;
        CapacityKg = string.Empty;
        VolumeM3 = string.Empty;
        SelectedBodyTypeCode = "van";
        ProductionYear = string.Empty;
        SelectedStatusCode = "available";
        InsuranceExpiry = null;
        SelectedCurrentDriverId = AdminLookupSelectionHelper.NormalizeOptionalSelection(0, DriverOptions);
        Notes = string.Empty;
        _deleteArmedVehicleId = null;
    }

    private async Task LoadSelectedVehicleAsync(AdminVehicleRowViewModel? vehicle)
    {
        if (vehicle is null)
        {
            ResetForm();
            return;
        }

        IsBusy = true;

        try
        {
            AdminVehicleEditData? data = await AdminCrudService.GetVehicleEditDataAsync(vehicle.Id);
            if (data is null)
            {
                StatusMessage = "Транспорт не найден.";
                return;
            }

            EditingVehicleId = data.Id;
            await LoadLookupsAsync();
            LicensePlate = data.LicensePlate;
            Model = data.Model;
            CapacityKg = AdminParsingHelper.FormatDecimal(data.CapacityKg);
            VolumeM3 = AdminParsingHelper.FormatDecimal(data.VolumeM3);
            SelectedBodyTypeCode = data.BodyType ?? "van";
            ProductionYear = data.ProductionYear?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
            SelectedStatusCode = data.Status;
            InsuranceExpiry = data.InsuranceExpiry;
            SelectedCurrentDriverId = data.CurrentDriverId;
            Notes = data.Notes ?? string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки транспорта: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSave() =>
        !IsBusy
        && !string.IsNullOrWhiteSpace(LicensePlate)
        && !string.IsNullOrWhiteSpace(Model)
        && AdminParsingHelper.TryParseRequiredDecimal(CapacityKg, out _, out _)
        && AdminParsingHelper.TryParseNullableDecimal(VolumeM3, out _, out _)
        && (string.IsNullOrWhiteSpace(ProductionYear) || ushort.TryParse(ProductionYear, NumberStyles.Integer, CultureInfo.CurrentCulture, out _));

    private async Task SaveAsync()
    {
        if (!AdminParsingHelper.TryParseRequiredDecimal(CapacityKg, out decimal capacity, out string? capacityError))
        {
            StatusMessage = capacityError;
            return;
        }

        if (!AdminParsingHelper.TryParseNullableDecimal(VolumeM3, out decimal? volume, out string? volumeError))
        {
            StatusMessage = volumeError;
            return;
        }

        if (!AdminParsingHelper.TryParseNullableUShort(ProductionYear, out ushort? productionYear, out string? yearError))
        {
            StatusMessage = yearError;
            return;
        }

        var data = new AdminVehicleEditData(
            EditingVehicleId,
            Model,
            LicensePlate,
            capacity,
            volume,
            SelectedBodyTypeCode,
            productionYear,
            SelectedStatusCode,
            InsuranceExpiry,
            SelectedCurrentDriverId is > 0 ? SelectedCurrentDriverId : null,
            Notes);

        if (EditingVehicleId.HasValue)
        {
            await ExecuteCrudAsync(() => AdminCrudService.UpdateVehicleAsync(data), "Транспорт обновлен.");
            return;
        }

        if (await ExecuteCrudAsync(() => AdminCrudService.CreateVehicleAsync(data), "Транспорт создан."))
        {
            ResetForm();
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedVehicle is null)
        {
            return;
        }

        if (_deleteArmedVehicleId != SelectedVehicle.Id)
        {
            _deleteArmedVehicleId = SelectedVehicle.Id;
            StatusMessage = $"Подтверждение: нажми Удалить еще раз, чтобы удалить транспорт {SelectedVehicle.LicensePlate}.";
            return;
        }

        uint vehicleId = SelectedVehicle.Id;
        if (await ExecuteCrudAsync(() => AdminCrudService.DeleteVehicleAsync(vehicleId), "Транспорт удален."))
        {
            ResetForm();
        }
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

internal static class AdminLookupHelper
{
    public static IEnumerable<AdminLookupItemViewModel> WithEmpty(IReadOnlyList<AdminLookupItemData> items, string emptyText)
    {
        yield return new AdminLookupItemViewModel(0, emptyText);

        foreach (AdminLookupItemData item in items)
        {
            yield return new AdminLookupItemViewModel(item.Id, item.DisplayName);
        }
    }
}

internal static class AdminLookupSelectionHelper
{
    public static uint? NormalizeOptionalSelection(uint? selectedId, IEnumerable<AdminLookupItemViewModel> options)
    {
        if (selectedId.HasValue && options.Any(x => x.Id == selectedId.Value))
        {
            return selectedId;
        }

        return options.FirstOrDefault()?.Id;
    }

    public static uint? NormalizeRequiredSelection(uint? selectedId, IEnumerable<AdminLookupItemViewModel> options)
    {
        if (selectedId is > 0 && options.Any(x => x.Id == selectedId.Value))
        {
            return selectedId;
        }

        return options.FirstOrDefault(x => x.Id != 0)?.Id;
    }
}

internal static class AdminSieveHelper
{
    public static string? JoinFilters(IEnumerable<string> filters)
    {
        string joinedFilters = string.Join(",", filters.Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(joinedFilters) ? null : joinedFilters;
    }

    public static string? NormalizeSort(string? sort)
    {
        return string.IsNullOrWhiteSpace(sort) ? null : sort.Trim();
    }

    public static string NormalizeValue(string value)
    {
        return value
            .Trim()
            .Replace(",", " ")
            .Replace("|", " ")
            .Replace("(", " ")
            .Replace(")", " ");
    }
}

internal static class AdminParsingHelper
{
    public static bool TryParseRequiredDecimal(string value, out decimal result, out string? errorMessage)
    {
        if (!TryParseNullableDecimal(value, out decimal? parsed, out errorMessage) || !parsed.HasValue)
        {
            result = 0;
            errorMessage ??= "Заполни числовое поле.";
            return false;
        }

        result = parsed.Value;
        return true;
    }

    public static bool TryParseNullableDecimal(string value, out decimal? result, out string? errorMessage)
    {
        result = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal currentResult)
            || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out currentResult))
        {
            result = currentResult;
            return true;
        }

        errorMessage = $"Не удалось прочитать число: {value}.";
        return false;
    }

    public static bool TryParseNullableUShort(string value, out ushort? result, out string? errorMessage)
    {
        result = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (ushort.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out ushort parsed))
        {
            result = parsed;
            return true;
        }

        errorMessage = $"Не удалось прочитать целое число: {value}.";
        return false;
    }

    public static string FormatDecimal(decimal? value) =>
        value.HasValue
            ? value.Value.ToString("0.##", CultureInfo.CurrentCulture)
            : string.Empty;

    public static string FormatDecimal(decimal value) =>
        value.ToString("0.##", CultureInfo.CurrentCulture);
}
