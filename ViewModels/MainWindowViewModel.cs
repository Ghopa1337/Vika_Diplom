using CargoTransport.Desktop.Services;

namespace CargoTransport.Desktop.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IAuthStateService _authStateService;

    public MainWindowViewModel(IAuthStateService authStateService)
    {
        _authStateService = authStateService;
        _authStateService.AuthStateChanged += HandleAuthStateChanged;
    }

    public string Title => "Система учета грузоперевозок";
    public string Subtitle => "WPF, MVVM, Material Design и локальное подключение к MariaDB";
    public string CurrentUserName => _authStateService.CurrentUser?.FullName ?? "Пользователь не определен";
    public string CurrentUserRole => _authStateService.CurrentUser?.RoleName ?? "Роль не определена";

    public string PlaceholderText =>
        "Главное окно пока специально оставлено пустым: база подключена, авторизация работает, " +
        "текущий пользователь определяется через сервис состояния. Дальше сюда спокойно навешиваем " +
        "дашборд и модули заказов, транспорта, водителей и пользователей.";

    private void HandleAuthStateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(CurrentUserName));
        OnPropertyChanged(nameof(CurrentUserRole));
    }
}
