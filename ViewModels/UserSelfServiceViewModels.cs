using System.Collections.ObjectModel;
using System.Globalization;
using CargoTransport.Desktop.Services;

namespace CargoTransport.Desktop.ViewModels;

public sealed class UserProfileSectionViewModel : ViewModelBase
{
    private readonly IUserSelfService _userSelfService;
    private readonly Func<Task> _onStateChanged;
    private string _username = string.Empty;
    private string _roleName = string.Empty;
    private string _fullName = string.Empty;
    private string _email = string.Empty;
    private string _phone = string.Empty;
    private string _companyName = string.Empty;
    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _accountStatus = string.Empty;
    private string _lastLoginLabel = "Нет данных";
    private string _createdAtLabel = "Нет данных";
    private string _updatedAtLabel = "Нет данных";
    private string _driverLicenseLabel = "Не указано";
    private string _driverStatusLabel = "Не указано";
    private string _statusMessage = string.Empty;
    private bool _mustChangePassword;
    private bool _isDriverProfileVisible;
    private bool _isBusy;

    public UserProfileSectionViewModel(
        IUserSelfService userSelfService,
        string title,
        string subtitle,
        Func<Task> onStateChanged)
    {
        _userSelfService = userSelfService;
        Title = title;
        Subtitle = subtitle;
        _onStateChanged = onStateChanged;

        RefreshCommand = new AsyncRelayCommand(LoadAsync, () => !IsBusy);
        SaveProfileCommand = new AsyncRelayCommand(SaveProfileAsync, () => !IsBusy);
        ChangePasswordCommand = new AsyncRelayCommand(ChangePasswordAsync, () => !IsBusy);
        ClearPasswordCommand = new RelayCommand(ClearPasswordFields, () => !IsBusy);
    }

    public string Title { get; }
    public string Subtitle { get; }
    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand SaveProfileCommand { get; }
    public AsyncRelayCommand ChangePasswordCommand { get; }
    public RelayCommand ClearPasswordCommand { get; }

    public string Username
    {
        get => _username;
        private set => Set(ref _username, value);
    }

    public string RoleName
    {
        get => _roleName;
        private set => Set(ref _roleName, value);
    }

    public string FullName
    {
        get => _fullName;
        set => Set(ref _fullName, value);
    }

    public string Email
    {
        get => _email;
        set => Set(ref _email, value);
    }

    public string Phone
    {
        get => _phone;
        set => Set(ref _phone, value);
    }

    public string CompanyName
    {
        get => _companyName;
        set => Set(ref _companyName, value);
    }

    public string CurrentPassword
    {
        get => _currentPassword;
        set => Set(ref _currentPassword, value);
    }

    public string NewPassword
    {
        get => _newPassword;
        set => Set(ref _newPassword, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => Set(ref _confirmPassword, value);
    }

    public string AccountStatus
    {
        get => _accountStatus;
        private set => Set(ref _accountStatus, value);
    }

    public string LastLoginLabel
    {
        get => _lastLoginLabel;
        private set => Set(ref _lastLoginLabel, value);
    }

    public string CreatedAtLabel
    {
        get => _createdAtLabel;
        private set => Set(ref _createdAtLabel, value);
    }

    public string UpdatedAtLabel
    {
        get => _updatedAtLabel;
        private set => Set(ref _updatedAtLabel, value);
    }

    public string DriverLicenseLabel
    {
        get => _driverLicenseLabel;
        private set => Set(ref _driverLicenseLabel, value);
    }

    public string DriverStatusLabel
    {
        get => _driverStatusLabel;
        private set => Set(ref _driverStatusLabel, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => Set(ref _statusMessage, value);
    }

    public bool MustChangePassword
    {
        get => _mustChangePassword;
        private set => Set(ref _mustChangePassword, value);
    }

    public bool IsDriverProfileVisible
    {
        get => _isDriverProfileVisible;
        private set => Set(ref _isDriverProfileVisible, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => Set(ref _isBusy, value);
    }

    public async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            ApplyProfile(await _userSelfService.GetProfileAsync());
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки профиля: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveProfileAsync()
    {
        try
        {
            IsBusy = true;
            UserProfileData profile = await _userSelfService.UpdateProfileAsync(new UserProfileUpdateData(
                FullName,
                Email,
                Phone,
                CompanyName));

            ApplyProfile(profile);
            StatusMessage = "Профиль обновлен.";
            await _onStateChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка сохранения: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ChangePasswordAsync()
    {
        try
        {
            IsBusy = true;
            await _userSelfService.ChangePasswordAsync(new UserPasswordChangeData(
                CurrentPassword,
                NewPassword,
                ConfirmPassword));

            ClearPasswordFields();
            await LoadAsync();
            StatusMessage = "Пароль изменен. В центре уведомлений добавлена запись безопасности.";
            await _onStateChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка смены пароля: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ClearPasswordFields()
    {
        CurrentPassword = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
    }

    private void ApplyProfile(UserProfileData profile)
    {
        Username = profile.Username;
        RoleName = profile.RoleName;
        FullName = profile.FullName;
        Email = profile.Email ?? string.Empty;
        Phone = profile.Phone ?? string.Empty;
        CompanyName = profile.CompanyName ?? string.Empty;
        MustChangePassword = profile.MustChangePassword;
        AccountStatus = profile.IsBlocked
            ? "Заблокирован"
            : profile.IsActive ? "Активен" : "Отключен";
        LastLoginLabel = FormatDate(profile.LastLoginAt);
        CreatedAtLabel = FormatDate(profile.CreatedAt);
        UpdatedAtLabel = FormatDate(profile.UpdatedAt);
        IsDriverProfileVisible = string.Equals(profile.RoleCode, "driver", StringComparison.OrdinalIgnoreCase);
        DriverLicenseLabel = string.IsNullOrWhiteSpace(profile.DriverLicenseNumber)
            ? "Не указано"
            : $"{profile.DriverLicenseNumber} / {profile.DriverLicenseCategory}";
        DriverStatusLabel = string.IsNullOrWhiteSpace(profile.DriverStatus)
            ? "Не указано"
            : $"{GetDriverStatusName(profile.DriverStatus)} • стаж {profile.DriverExperienceYears ?? 0} лет";
    }

    private void RaiseCommandStates()
    {
        RefreshCommand.RaiseCanExecuteChanged();
        SaveProfileCommand.RaiseCanExecuteChanged();
        ChangePasswordCommand.RaiseCanExecuteChanged();
        ClearPasswordCommand.RaiseCanExecuteChanged();
    }

    protected override void OnPropertyChanged(string? propertyName)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(IsBusy))
        {
            RaiseCommandStates();
        }
    }

    private static string FormatDate(DateTime? value) =>
        value?.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("ru-RU")) ?? "Нет данных";

    private static string GetDriverStatusName(string status) =>
        status switch
        {
            "available" => "Доступен",
            "on_route" => "В рейсе",
            "rest" => "Отдых",
            "sick" => "Болен",
            _ => status
        };
}

public sealed class UserNotificationRowViewModel
{
    public required uint Id { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required string TypeName { get; init; }
    public required string CreatedAtLabel { get; init; }
    public required string ReadState { get; init; }
    public required string ReadAtLabel { get; init; }
    public required bool IsRead { get; init; }
}

public sealed class UserNotificationsSectionViewModel : ViewModelBase
{
    private readonly IUserSelfService _userSelfService;
    private readonly Func<Task> _onStateChanged;
    private UserNotificationRowViewModel? _selectedNotification;
    private string _statusMessage = string.Empty;
    private bool _unreadOnly;
    private bool _isBusy;
    private int _totalCount;
    private int _unreadCount;

    public UserNotificationsSectionViewModel(
        IUserSelfService userSelfService,
        string title,
        string subtitle,
        Func<Task> onStateChanged)
    {
        _userSelfService = userSelfService;
        Title = title;
        Subtitle = subtitle;
        _onStateChanged = onStateChanged;

        RefreshCommand = new AsyncRelayCommand(LoadAsync, () => !IsBusy);
        ApplyFilterCommand = new AsyncRelayCommand(LoadAsync, () => !IsBusy);
        ClearFilterCommand = new AsyncRelayCommand(ClearFilterAsync, () => !IsBusy);
        MarkSelectedReadCommand = new AsyncRelayCommand(MarkSelectedReadAsync, () => !IsBusy && SelectedNotification is not null && !SelectedNotification.IsRead);
        MarkAllReadCommand = new AsyncRelayCommand(MarkAllReadAsync, () => !IsBusy && UnreadCount > 0);
    }

    public string Title { get; }
    public string Subtitle { get; }
    public ObservableCollection<UserNotificationRowViewModel> Notifications { get; } = [];
    public ObservableCollection<AdminStatTileViewModel> Metrics { get; } = [];
    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand ApplyFilterCommand { get; }
    public AsyncRelayCommand ClearFilterCommand { get; }
    public AsyncRelayCommand MarkSelectedReadCommand { get; }
    public AsyncRelayCommand MarkAllReadCommand { get; }

    public bool UnreadOnly
    {
        get => _unreadOnly;
        set => Set(ref _unreadOnly, value);
    }

    public UserNotificationRowViewModel? SelectedNotification
    {
        get => _selectedNotification;
        set => Set(ref _selectedNotification, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => Set(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => Set(ref _isBusy, value);
    }

    public int TotalCount
    {
        get => _totalCount;
        private set => Set(ref _totalCount, value);
    }

    public int UnreadCount
    {
        get => _unreadCount;
        private set => Set(ref _unreadCount, value);
    }

    public string SelectedTitle => SelectedNotification?.Title ?? "Уведомление не выбрано";
    public string SelectedMessage => SelectedNotification?.Message ?? "Выберите запись в ленте, чтобы посмотреть текст уведомления.";
    public string SelectedMeta => SelectedNotification is null
        ? "Нет выбранной записи"
        : $"{SelectedNotification.TypeName} • {SelectedNotification.CreatedAtLabel} • {SelectedNotification.ReadState}";

    public async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            IReadOnlyList<UserNotificationData> notifications = await _userSelfService.GetNotificationsAsync(UnreadOnly);
            IReadOnlyList<UserNotificationData> allNotifications = UnreadOnly
                ? await _userSelfService.GetNotificationsAsync(unreadOnly: false)
                : notifications;

            TotalCount = allNotifications.Count;
            UnreadCount = allNotifications.Count(x => !x.IsRead);

            List<UserNotificationRowViewModel> rows = notifications.Select(MapNotification).ToList();
            AdminCollectionHelper.ReplaceWith(Notifications, rows);
            SelectedNotification = rows.FirstOrDefault(x => SelectedNotification is not null && x.Id == SelectedNotification.Id)
                ?? rows.FirstOrDefault();
            ApplyMetrics();
            StatusMessage = rows.Count == 0
                ? (UnreadOnly ? "Непрочитанных уведомлений нет." : "Уведомлений пока нет.")
                : string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки уведомлений: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ClearFilterAsync()
    {
        UnreadOnly = false;
        await LoadAsync();
    }

    private async Task MarkSelectedReadAsync()
    {
        if (SelectedNotification is null)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await _userSelfService.MarkNotificationReadAsync(SelectedNotification.Id);
            await LoadAsync();
            StatusMessage = "Уведомление отмечено как прочитанное.";
            await _onStateChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка обновления уведомления: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task MarkAllReadAsync()
    {
        try
        {
            IsBusy = true;
            await _userSelfService.MarkAllNotificationsReadAsync();
            await LoadAsync();
            StatusMessage = "Все уведомления отмечены как прочитанные.";
            await _onStateChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка обновления уведомлений: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override void OnPropertyChanged(string? propertyName)
    {
        base.OnPropertyChanged(propertyName);

        switch (propertyName)
        {
            case nameof(SelectedNotification):
                MarkSelectedReadCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(SelectedTitle));
                OnPropertyChanged(nameof(SelectedMessage));
                OnPropertyChanged(nameof(SelectedMeta));
                break;
            case nameof(IsBusy):
            case nameof(UnreadCount):
                RefreshCommand.RaiseCanExecuteChanged();
                ApplyFilterCommand.RaiseCanExecuteChanged();
                ClearFilterCommand.RaiseCanExecuteChanged();
                MarkSelectedReadCommand.RaiseCanExecuteChanged();
                MarkAllReadCommand.RaiseCanExecuteChanged();
                break;
        }
    }

    private void ApplyMetrics()
    {
        AdminCollectionHelper.ReplaceWith(
            Metrics,
            [
                new AdminStatTileViewModel("Всего", TotalCount.ToString(CultureInfo.CurrentCulture), "Все входящие уведомления текущего пользователя."),
                new AdminStatTileViewModel("Непрочитанные", UnreadCount.ToString(CultureInfo.CurrentCulture), "События, которые еще требуют внимания."),
                new AdminStatTileViewModel("Фильтр", UnreadOnly ? "Только новые" : "Вся лента", "Режим отображения центра уведомлений.")
            ]);
    }

    private static UserNotificationRowViewModel MapNotification(UserNotificationData notification) =>
        new()
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            TypeName = GetTypeName(notification.NotificationType),
            CreatedAtLabel = notification.CreatedAt.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("ru-RU")),
            ReadAtLabel = notification.ReadAt?.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("ru-RU")) ?? "Не прочитано",
            ReadState = notification.IsRead ? "Прочитано" : "Новое",
            IsRead = notification.IsRead
        };

    private static string GetTypeName(string type) =>
        type switch
        {
            "order" => "Заказ",
            "security" => "Безопасность",
            "route" => "Маршрут",
            "system" => "Система",
            _ => type
        };
}
