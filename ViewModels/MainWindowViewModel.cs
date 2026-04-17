using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CargoTransport.Desktop.Models;
using CargoTransport.Desktop.Services;

namespace CargoTransport.Desktop.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IAuthStateService _authStateService;
    private readonly IAdminPanelService _adminPanelService;
    private readonly IAdminCrudService _adminCrudService;
    private readonly IRoleOrderWorkspaceService _roleOrderWorkspaceService;
    private readonly AdminDashboardSectionViewModel _dashboardSection;
    private readonly AdminUsersSectionViewModel _usersSection;
    private readonly AdminOrdersSectionViewModel _ordersSection;
    private readonly AdminCargoSectionViewModel _cargoSection;
    private readonly AdminDriversSectionViewModel _driversSection;
    private readonly AdminVehiclesSectionViewModel _vehiclesSection;
    private readonly AdminReportsSectionViewModel _reportsSection;
    private readonly RoleDashboardSectionViewModel _dispatcherDashboardSection;
    private readonly RoleDashboardSectionViewModel _receiverDashboardSection;
    private readonly RoleDashboardSectionViewModel _driverDashboardSection;
    private readonly RoleOrdersSectionViewModel _receiverOrdersSection;
    private readonly RoleChecklistSectionViewModel _receiverProfileSection;
    private readonly RoleChecklistSectionViewModel _receiverNotificationsSection;
    private readonly RoleOrdersSectionViewModel _driverOrdersSection;
    private readonly RoleChecklistSectionViewModel _driverProfileSection;
    private readonly RoleChecklistSectionViewModel _driverNotificationsSection;

    private readonly AdminNavigationItemViewModel _dashboardNavigationItem;
    private readonly AdminNavigationItemViewModel _usersNavigationItem;
    private readonly AdminNavigationItemViewModel _ordersNavigationItem;
    private readonly AdminNavigationItemViewModel _cargoNavigationItem;
    private readonly AdminNavigationItemViewModel _driversNavigationItem;
    private readonly AdminNavigationItemViewModel _vehiclesNavigationItem;
    private readonly AdminNavigationItemViewModel _reportsNavigationItem;
    private readonly AdminNavigationItemViewModel _dispatcherDashboardNavigationItem;
    private readonly AdminNavigationItemViewModel _dispatcherOrdersNavigationItem;
    private readonly AdminNavigationItemViewModel _dispatcherCargoNavigationItem;
    private readonly AdminNavigationItemViewModel _dispatcherDriversNavigationItem;
    private readonly AdminNavigationItemViewModel _dispatcherVehiclesNavigationItem;
    private readonly AdminNavigationItemViewModel _dispatcherReportsNavigationItem;
    private readonly AdminNavigationItemViewModel _receiverDashboardNavigationItem;
    private readonly AdminNavigationItemViewModel _receiverOrdersNavigationItem;
    private readonly AdminNavigationItemViewModel _receiverProfileNavigationItem;
    private readonly AdminNavigationItemViewModel _receiverNotificationsNavigationItem;
    private readonly AdminNavigationItemViewModel _driverDashboardNavigationItem;
    private readonly AdminNavigationItemViewModel _driverOrdersNavigationItem;
    private readonly AdminNavigationItemViewModel _driverProfileNavigationItem;
    private readonly AdminNavigationItemViewModel _driverNotificationsNavigationItem;

    private AdminNavigationItemViewModel? _selectedNavigationItem;
    private bool _isInitialized;
    private object? _activeModalContent;

    public MainWindowViewModel(
        IAuthStateService authStateService,
        IAdminPanelService adminPanelService,
        IAdminCrudService adminCrudService,
        IRoleOrderWorkspaceService roleOrderWorkspaceService)
    {
        _authStateService = authStateService;
        _adminPanelService = adminPanelService;
        _adminCrudService = adminCrudService;
        _roleOrderWorkspaceService = roleOrderWorkspaceService;
        _authStateService.AuthStateChanged += HandleAuthStateChanged;

        _dashboardSection = new AdminDashboardSectionViewModel();
        _usersSection = new AdminUsersSectionViewModel(_adminCrudService, ReloadAdminPanelAsync);
        _ordersSection = new AdminOrdersSectionViewModel(_adminCrudService, ReloadAdminPanelAsync, OpenOrderWizard, OpenOrderDetails);
        _cargoSection = new AdminCargoSectionViewModel(_adminCrudService, ReloadAdminPanelAsync);
        _driversSection = new AdminDriversSectionViewModel(_adminCrudService, ReloadAdminPanelAsync);
        _vehiclesSection = new AdminVehiclesSectionViewModel(_adminCrudService, ReloadAdminPanelAsync);
        _reportsSection = new AdminReportsSectionViewModel();
        _dispatcherDashboardSection = new RoleDashboardSectionViewModel(
            "Пульт диспетчера",
            "Оперативная рабочая зона для назначения водителей, контроля статусов рейсов и мониторинга автопарка.",
            "Фокус смены",
            "Сначала проверьте новые заказы, затем закрепите доступный экипаж и транспорт.");
        _receiverDashboardSection = new RoleDashboardSectionViewModel(
            "Кабинет получателя",
            "Отдельное пространство для отслеживания доставок, подтверждения получения и связи по заказам.",
            "Что появится дальше",
            "На следующем этапе здесь заработает личный список заказов и карточка подтверждения получения.");
        _driverDashboardSection = new RoleDashboardSectionViewModel(
            "Кабинет водителя",
            "Рабочее место для рейсов, статусов перевозки и оперативных уведомлений по маршрутам.",
            "Что появится дальше",
            "Следующим шагом сюда подключим мои рейсы, статусы загрузки и подтверждение доставки.");

        _receiverOrdersSection = new RoleOrdersSectionViewModel(
            _roleOrderWorkspaceService,
            RoleOrderCabinetMode.Receiver,
            OpenOrderDetails,
            UpdateSelfServiceShellAsync);
        _receiverProfileSection = new RoleChecklistSectionViewModel(
            "Профиль получателя",
            "Личный раздел под контактные данные, компанию и будущую смену пароля.",
            "Дальше по ТЗ",
            "После заказов сюда добавим сценарии профиля, смены пароля и персональные настройки.",
            [
                new RoleChecklistItemViewModel("Карточка пользователя", "ФИО, компания, телефон и email.", "Запланировано"),
                new RoleChecklistItemViewModel("Смена пароля", "Отдельный безопасный сценарий, а не поле в CRUD.", "Запланировано")
            ]);
        _receiverNotificationsSection = new RoleChecklistSectionViewModel(
            "Уведомления",
            "Отдельная секция для входящих уведомлений получателя о назначении, прибытии и завершении заказа.",
            "База уже есть",
            "Модель уведомлений в проекте существует, дальше подключим выборку и отметку прочитанного.",
            [
                new RoleChecklistItemViewModel("Лента уведомлений", "Новые и прочитанные события по заказам.", "Запланировано"),
                new RoleChecklistItemViewModel("Отметка прочтения", "Изменение статуса уведомления внутри кабинета.", "Запланировано")
            ]);

        _driverOrdersSection = new RoleOrdersSectionViewModel(
            _roleOrderWorkspaceService,
            RoleOrderCabinetMode.Driver,
            OpenOrderDetails,
            UpdateSelfServiceShellAsync);
        _driverProfileSection = new RoleChecklistSectionViewModel(
            "Профиль водителя",
            "Раздел под персональные данные, водительское удостоверение и рабочую информацию.",
            "Дальше по ТЗ",
            "После личного кабинета рейсов сюда добавим профиль и самостоятельную смену пароля.",
            [
                new RoleChecklistItemViewModel("Личные данные", "ФИО, телефон и служебные контакты.", "Запланировано"),
                new RoleChecklistItemViewModel("Данные ВУ", "Номер, категория и срок актуальности.", "Запланировано")
            ]);
        _driverNotificationsSection = new RoleChecklistSectionViewModel(
            "Уведомления",
            "Отдельный центр уведомлений водителя под новые назначения и изменения по маршруту.",
            "База уже есть",
            "На следующем шаге подключим выборку из таблицы уведомлений и быстрые переходы к рейсам.",
            [
                new RoleChecklistItemViewModel("Новые назначения", "Сообщения о закреплении нового рейса.", "Запланировано"),
                new RoleChecklistItemViewModel("Изменения маршрута", "Уведомления о переносе, отмене и смене статуса.", "Запланировано")
            ]);

        _dashboardNavigationItem = new AdminNavigationItemViewModel("Дашборд", "Оперативная сводка по системе и последним действиям", "0", _dashboardSection);
        _usersNavigationItem = new AdminNavigationItemViewModel("Пользователи", "Управление учетными записями и ролями", "0", _usersSection);
        _ordersNavigationItem = new AdminNavigationItemViewModel("Заказы", "Фильтрация, карточки и контроль статусов перевозок", "0", _ordersSection);
        _cargoNavigationItem = new AdminNavigationItemViewModel("Грузы", "Справочник грузов, типов и требований к перевозке", "0", _cargoSection);
        _driversNavigationItem = new AdminNavigationItemViewModel("Водители", "Загрузка, доступность и история выполнения", "0", _driversSection);
        _vehiclesNavigationItem = new AdminNavigationItemViewModel("Транспорт", "Состояние автопарка и закрепление ТС", "0", _vehiclesSection);
        _reportsNavigationItem = new AdminNavigationItemViewModel("Отчеты", "Подготовка аналитики и экспортных сводок", "0", _reportsSection);
        _dispatcherDashboardNavigationItem = new AdminNavigationItemViewModel("Смена", "Ключевые показатели диспетчера и быстрые действия по логистике", "0", _dispatcherDashboardSection);
        _dispatcherOrdersNavigationItem = new AdminNavigationItemViewModel("Заказы", "Назначение исполнителей, контроль доставки и статусов", "0", _ordersSection);
        _dispatcherCargoNavigationItem = new AdminNavigationItemViewModel("Грузы", "Справочник грузов для быстрого подбора под новые заказы", "0", _cargoSection);
        _dispatcherDriversNavigationItem = new AdminNavigationItemViewModel("Водители", "Доступность водителей и распределение загрузки", "0", _driversSection);
        _dispatcherVehiclesNavigationItem = new AdminNavigationItemViewModel("Транспорт", "Подбор ТС под рейс и контроль готовности", "0", _vehiclesSection);
        _dispatcherReportsNavigationItem = new AdminNavigationItemViewModel("Отчеты", "Оперативные сводки по смене и работе парка", "0", _reportsSection);
        _receiverDashboardNavigationItem = new AdminNavigationItemViewModel("Обзор", "Персональная сводка по доставкам и входящим событиям", "роль", _receiverDashboardSection);
        _receiverOrdersNavigationItem = new AdminNavigationItemViewModel("Мои заказы", "Личный кабинет получателя с карточками доставок", "скоро", _receiverOrdersSection);
        _receiverProfileNavigationItem = new AdminNavigationItemViewModel("Профиль", "Контакты, компания и настройки учетной записи", "скоро", _receiverProfileSection);
        _receiverNotificationsNavigationItem = new AdminNavigationItemViewModel("Уведомления", "События по заказам и подтверждениям", "скоро", _receiverNotificationsSection);
        _driverDashboardNavigationItem = new AdminNavigationItemViewModel("Обзор", "Персональная сводка по рейсам и рабочим событиям", "роль", _driverDashboardSection);
        _driverOrdersNavigationItem = new AdminNavigationItemViewModel("Мои рейсы", "Назначенные перевозки и рабочие статусы водителя", "скоро", _driverOrdersSection);
        _driverProfileNavigationItem = new AdminNavigationItemViewModel("Профиль", "Личные данные, ВУ и настройки учетной записи", "скоро", _driverProfileSection);
        _driverNotificationsNavigationItem = new AdminNavigationItemViewModel("Уведомления", "Новые назначения и изменения по маршрутам", "скоро", _driverNotificationsSection);

        NavigationItems = [];
        ConfigureShellForCurrentRole();
    }

    public ObservableCollection<AdminNavigationItemViewModel> NavigationItems { get; }

    public AdminNavigationItemViewModel? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set => Set(ref _selectedNavigationItem, value);
    }

    public object? SelectedContent => SelectedNavigationItem?.Content;
    public object? ActiveModalContent
    {
        get => _activeModalContent;
        set => Set(ref _activeModalContent, value);
    }

    public string ShellTitle => CurrentRoleCode switch
    {
        "dispatcher" => "Рабочее место диспетчера",
        "receiver" => "Кабинет получателя",
        "driver" => "Кабинет водителя",
        _ => "Панель администратора"
    };
    public string ShellSubtitle => CurrentRoleCode switch
    {
        "dispatcher" => "Оперативное управление рейсами, назначением водителей и парком транспорта.",
        "receiver" => "Персональное пространство для отслеживания доставок и подтверждения получения.",
        "driver" => "Персональное пространство для рейсов, статусов перевозки и уведомлений.",
        _ => "Контроль пользователей, заказов, транспорта и водителей в одной рабочей среде"
    };
    public string SelectedNavigationTitle => SelectedNavigationItem?.Title ?? ShellTitle;
    public string SelectedNavigationSubtitle => SelectedNavigationItem?.Subtitle ?? ShellSubtitle;
    public string TodayLabel => DateTime.Now.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("ru-RU"));
    public string CurrentUserName => _authStateService.CurrentUser?.FullName ?? "Пользователь не определен";
    public string CurrentUserRole => _authStateService.CurrentUser?.RoleName ?? "Роль не определена";
    public string CurrentUserLogin => _authStateService.CurrentUser?.Username ?? "unknown";
    public string SessionLabel => CurrentRoleCode switch
    {
        "dispatcher" => "Операционный доступ диспетчера",
        "receiver" => "Персональный доступ получателя",
        "driver" => "Персональный доступ водителя",
        _ => "Полный доступ к системе"
    };

    private string CurrentRoleCode => _authStateService.CurrentUser?.RoleCode ?? "admin";

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

        await ReloadAdminPanelAsync(cancellationToken);
        _isInitialized = true;
    }

    public async Task ReloadAdminPanelAsync(CancellationToken cancellationToken = default)
    {
        ConfigureShellForCurrentRole();

        if (CurrentRoleCode == "receiver")
        {
            await _receiverOrdersSection.LoadAsync(cancellationToken);
            await UpdateSelfServiceShellAsync();
            return;
        }

        if (CurrentRoleCode == "driver")
        {
            await _driverOrdersSection.LoadAsync(cancellationToken);
            await UpdateSelfServiceShellAsync();
            return;
        }

        var query = new AdminPanelQuery(
            _usersSection.BuildSieveModel(),
            _ordersSection.BuildSieveModel(),
            _cargoSection.BuildSieveModel(),
            _driversSection.BuildSieveModel(),
            _vehiclesSection.BuildSieveModel());

        AdminPanelData panelData = await _adminPanelService.GetAdminPanelDataAsync(query, cancellationToken);

        _dashboardSection.ApplyData(panelData.Dashboard);
        _usersSection.ApplyData(panelData.Users);
        _ordersSection.ApplyData(panelData.Orders);
        _cargoSection.ApplyData(panelData.Cargo);
        _driversSection.ApplyData(panelData.Drivers);
        _vehiclesSection.ApplyData(panelData.Vehicles);
        _reportsSection.ApplyData(panelData.Reports);

        await _usersSection.LoadLookupsAsync(cancellationToken);
        await _ordersSection.LoadLookupsAsync(cancellationToken);
        await _cargoSection.LoadLookupsAsync(cancellationToken);
        await _driversSection.LoadLookupsAsync(cancellationToken);
        await _vehiclesSection.LoadLookupsAsync(cancellationToken);

        _dashboardNavigationItem.Badge = $"{panelData.Dashboard.RecentActivities.Count}";
        _usersNavigationItem.Badge = $"{panelData.Users.Users.Count}";
        _ordersNavigationItem.Badge = $"{panelData.Orders.Orders.Count}";
        _cargoNavigationItem.Badge = $"{panelData.Cargo.Cargo.Count}";
        _driversNavigationItem.Badge = $"{panelData.Drivers.Drivers.Count}";
        _vehiclesNavigationItem.Badge = $"{panelData.Vehicles.Vehicles.Count}";
        _reportsNavigationItem.Badge = $"{panelData.Reports.Reports.Count}";
        _dispatcherDashboardNavigationItem.Badge = $"{panelData.Orders.Orders.Count}";
        _dispatcherOrdersNavigationItem.Badge = $"{panelData.Orders.Orders.Count}";
        _dispatcherCargoNavigationItem.Badge = $"{panelData.Cargo.Cargo.Count}";
        _dispatcherDriversNavigationItem.Badge = $"{panelData.Drivers.Drivers.Count}";
        _dispatcherVehiclesNavigationItem.Badge = $"{panelData.Vehicles.Vehicles.Count}";
        _dispatcherReportsNavigationItem.Badge = $"{panelData.Reports.Reports.Count}";

        _dispatcherDashboardSection.Apply(
            [
                new AdminStatTileViewModel("Новых заказов", $"{panelData.Orders.Orders.Count}", "Текущий пул заказов для распределения и контроля."),
                new AdminStatTileViewModel("Водителей на линии", $"{panelData.Drivers.Drivers.Count}", "Доступные и занятые исполнители в текущей смене."),
                new AdminStatTileViewModel("Транспорт в парке", $"{panelData.Vehicles.Vehicles.Count}", "Машины, которые можно назначить на рейсы.")
            ],
            [
                new AdminQuickActionCardViewModel("Распределить новые заказы", "Откройте раздел заказов и закрепите водителя и транспорт за новыми заявками.", "Раздел Заказы"),
                new AdminQuickActionCardViewModel("Проверить доступность водителей", "Сверьте текущие статусы, номера ВУ и готовность к выезду.", "Раздел Водители"),
                new AdminQuickActionCardViewModel("Сверить парк ТС", "Уточните свободный транспорт и состояние закрепления за водителями.", "Раздел Транспорт")
            ]);
    }

    public void OpenOrderWizard(uint? receiverId = null)
    {
        var wizard = new OrderWizardViewModel(
            _adminCrudService,
            async () =>
            {
                ActiveModalContent = null;
                await ReloadAdminPanelAsync();
            },
            receiverId);

        _ = wizard.LoadLookupsAsync();
        ActiveModalContent = wizard;
    }

    public void OpenOrderDetails(Order order)
    {
        var details = new OrderDetailsViewModel(
            _adminCrudService,
            order,
            async () => {
                ActiveModalContent = null;
                await ReloadAdminPanelAsync();
            });

        _ = details.LoadDetailsAsync();
        ActiveModalContent = details;
    }

    private void HandleAuthStateChanged(object? sender, EventArgs e)
    {
        ConfigureShellForCurrentRole();
        OnPropertyChanged(nameof(CurrentUserName));
        OnPropertyChanged(nameof(CurrentUserRole));
        OnPropertyChanged(nameof(CurrentUserLogin));
        OnPropertyChanged(nameof(SessionLabel));
        OnPropertyChanged(nameof(ShellTitle));
        OnPropertyChanged(nameof(ShellSubtitle));
        OnPropertyChanged(nameof(SelectedNavigationTitle));
        OnPropertyChanged(nameof(SelectedNavigationSubtitle));

        if (_isInitialized)
        {
            _ = ReloadAdminPanelAsync();
        }
    }

    private void ConfigureShellForCurrentRole()
    {
        string? currentTitle = SelectedNavigationItem?.Title;

        NavigationItems.Clear();

        switch (CurrentRoleCode)
        {
            case "dispatcher":
                NavigationItems.Add(_dispatcherDashboardNavigationItem);
                NavigationItems.Add(_dispatcherOrdersNavigationItem);
                NavigationItems.Add(_dispatcherCargoNavigationItem);
                NavigationItems.Add(_dispatcherDriversNavigationItem);
                NavigationItems.Add(_dispatcherVehiclesNavigationItem);
                NavigationItems.Add(_dispatcherReportsNavigationItem);
                break;
            case "receiver":
                NavigationItems.Add(_receiverDashboardNavigationItem);
                NavigationItems.Add(_receiverOrdersNavigationItem);
                NavigationItems.Add(_receiverProfileNavigationItem);
                NavigationItems.Add(_receiverNotificationsNavigationItem);
                break;
            case "driver":
                NavigationItems.Add(_driverDashboardNavigationItem);
                NavigationItems.Add(_driverOrdersNavigationItem);
                NavigationItems.Add(_driverProfileNavigationItem);
                NavigationItems.Add(_driverNotificationsNavigationItem);
                break;
            default:
                NavigationItems.Add(_dashboardNavigationItem);
                NavigationItems.Add(_usersNavigationItem);
                NavigationItems.Add(_ordersNavigationItem);
                NavigationItems.Add(_cargoNavigationItem);
                NavigationItems.Add(_driversNavigationItem);
                NavigationItems.Add(_vehiclesNavigationItem);
                NavigationItems.Add(_reportsNavigationItem);
                break;
        }

        SelectedNavigationItem = NavigationItems.FirstOrDefault(x => x.Title == currentTitle) ?? NavigationItems.FirstOrDefault();
    }

    private Task UpdateSelfServiceShellAsync()
    {
        _receiverDashboardSection.Apply(
            _receiverOrdersSection.GetDashboardMetrics(),
            _receiverOrdersSection.GetDashboardQuickActions());

        _driverDashboardSection.Apply(
            _driverOrdersSection.GetDashboardMetrics(),
            _driverOrdersSection.GetDashboardQuickActions());

        _receiverDashboardNavigationItem.Badge = $"{_receiverOrdersSection.TotalOrdersCount}";
        _receiverOrdersNavigationItem.Badge = $"{_receiverOrdersSection.AwaitingActionCount}";
        _receiverProfileNavigationItem.Badge = "профиль";
        _receiverNotificationsNavigationItem.Badge = "скоро";
        _driverDashboardNavigationItem.Badge = $"{_driverOrdersSection.TotalOrdersCount}";
        _driverOrdersNavigationItem.Badge = $"{_driverOrdersSection.AwaitingActionCount}";
        _driverProfileNavigationItem.Badge = "профиль";
        _driverNotificationsNavigationItem.Badge = "скоро";

        return Task.CompletedTask;
    }
}
