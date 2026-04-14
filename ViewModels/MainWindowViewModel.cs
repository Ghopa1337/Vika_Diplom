using CargoTransport.Desktop.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace CargoTransport.Desktop.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IAuthStateService _authStateService;
    private readonly IAdminPanelService _adminPanelService;
    private readonly AdminDashboardSectionViewModel _dashboardSection;
    private readonly AdminUsersSectionViewModel _usersSection;
    private readonly AdminOrdersSectionViewModel _ordersSection;
    private readonly AdminDriversSectionViewModel _driversSection;
    private readonly AdminVehiclesSectionViewModel _vehiclesSection;
    private readonly AdminReportsSectionViewModel _reportsSection;

    private readonly AdminNavigationItemViewModel _dashboardNavigationItem;
    private readonly AdminNavigationItemViewModel _usersNavigationItem;
    private readonly AdminNavigationItemViewModel _ordersNavigationItem;
    private readonly AdminNavigationItemViewModel _driversNavigationItem;
    private readonly AdminNavigationItemViewModel _vehiclesNavigationItem;
    private readonly AdminNavigationItemViewModel _reportsNavigationItem;

    private AdminNavigationItemViewModel? _selectedNavigationItem;
    private bool _isInitialized;

    public MainWindowViewModel(
        IAuthStateService authStateService,
        IAdminPanelService adminPanelService)
    {
        _authStateService = authStateService;
        _adminPanelService = adminPanelService;
        _authStateService.AuthStateChanged += HandleAuthStateChanged;

        _dashboardSection = new AdminDashboardSectionViewModel();
        _usersSection = new AdminUsersSectionViewModel();
        _ordersSection = new AdminOrdersSectionViewModel();
        _driversSection = new AdminDriversSectionViewModel();
        _vehiclesSection = new AdminVehiclesSectionViewModel();
        _reportsSection = new AdminReportsSectionViewModel();

        _dashboardNavigationItem = new AdminNavigationItemViewModel("Дашборд", "Оперативная сводка по системе и последним действиям", "0", _dashboardSection);
        _usersNavigationItem = new AdminNavigationItemViewModel("Пользователи", "Управление учетными записями и ролями", "0", _usersSection);
        _ordersNavigationItem = new AdminNavigationItemViewModel("Заказы", "Фильтрация, карточки и контроль статусов перевозок", "0", _ordersSection);
        _driversNavigationItem = new AdminNavigationItemViewModel("Водители", "Загрузка, доступность и история выполнения", "0", _driversSection);
        _vehiclesNavigationItem = new AdminNavigationItemViewModel("Транспорт", "Состояние автопарка и закрепление ТС", "0", _vehiclesSection);
        _reportsNavigationItem = new AdminNavigationItemViewModel("Отчеты", "Подготовка аналитики и экспортных сводок", "0", _reportsSection);

        NavigationItems =
        [
            _dashboardNavigationItem,
            _usersNavigationItem,
            _ordersNavigationItem,
            _driversNavigationItem,
            _vehiclesNavigationItem,
            _reportsNavigationItem
        ];

        SelectedNavigationItem = _dashboardNavigationItem;
    }

    public ObservableCollection<AdminNavigationItemViewModel> NavigationItems { get; }

    public AdminNavigationItemViewModel? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set => Set(ref _selectedNavigationItem, value);
    }

    public object? SelectedContent => SelectedNavigationItem?.Content;
    public string ShellTitle => "Панель администратора";
    public string ShellSubtitle => "Контроль пользователей, заказов, транспорта и водителей в одной рабочей среде";
    public string SelectedNavigationTitle => SelectedNavigationItem?.Title ?? "Администрирование";
    public string SelectedNavigationSubtitle => SelectedNavigationItem?.Subtitle ?? "Рабочий раздел администратора";
    public string TodayLabel => DateTime.Now.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("ru-RU"));
    public string CurrentUserName => _authStateService.CurrentUser?.FullName ?? "Пользователь не определен";
    public string CurrentUserRole => _authStateService.CurrentUser?.RoleName ?? "Роль не определена";
    public string CurrentUserLogin => _authStateService.CurrentUser?.Username ?? "unknown";
    public string SessionLabel => _authStateService.CurrentUser?.RoleCode == "admin"
        ? "Полный доступ к системе"
        : "Режим администратора для прототипа";

    protected override void OnPropertyChanged(string? propertyName)
    {
        base.OnPropertyChanged(propertyName);

        switch (propertyName)
        {
            case nameof(SelectedNavigationItem):
                OnPropertyChanged(nameof(SelectedContent));
                OnPropertyChanged(nameof(SelectedNavigationTitle));
                OnPropertyChanged(nameof(SelectedNavigationSubtitle));
                break;
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        AdminPanelData panelData = await _adminPanelService.GetAdminPanelDataAsync(cancellationToken);

        _dashboardSection.ApplyData(panelData.Dashboard);
        _usersSection.ApplyData(panelData.Users);
        _ordersSection.ApplyData(panelData.Orders);
        _driversSection.ApplyData(panelData.Drivers);
        _vehiclesSection.ApplyData(panelData.Vehicles);
        _reportsSection.ApplyData(panelData.Reports);

        _dashboardNavigationItem.Badge = $"{panelData.Dashboard.RecentActivities.Count}";
        _usersNavigationItem.Badge = $"{panelData.Users.Users.Count}";
        _ordersNavigationItem.Badge = $"{panelData.Orders.Orders.Count}";
        _driversNavigationItem.Badge = $"{panelData.Drivers.Drivers.Count}";
        _vehiclesNavigationItem.Badge = $"{panelData.Vehicles.Vehicles.Count}";
        _reportsNavigationItem.Badge = $"{panelData.Reports.Reports.Count}";

        _isInitialized = true;
    }

    private void HandleAuthStateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(CurrentUserName));
        OnPropertyChanged(nameof(CurrentUserRole));
        OnPropertyChanged(nameof(CurrentUserLogin));
        OnPropertyChanged(nameof(SessionLabel));
    }
}
