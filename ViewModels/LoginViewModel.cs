using CargoTransport.Desktop.Services;
using System.Windows.Input;

namespace CargoTransport.Desktop.ViewModels;

public sealed class LoginViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IConfigService _configService;
    private readonly IWindowService _windowService;
    private readonly AsyncRelayCommand _loginCommand;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _rememberUsername;
    private bool _isBusy;

    public LoginViewModel(
        IAuthenticationService authenticationService,
        IConfigService configService,
        IWindowService windowService)
    {
        _authenticationService = authenticationService;
        _configService = configService;
        _windowService = windowService;

        _loginCommand = new AsyncRelayCommand(LoginAsync, CanLogin);
        Username = _configService.GetValue("Auth.LastUsername") ?? string.Empty;
        RememberUsername = !string.IsNullOrWhiteSpace(Username);
    }

    public ICommand LoginCommand => _loginCommand;

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

    public string ErrorMessage
    {
        get => _errorMessage;
        set => Set(ref _errorMessage, value);
    }

    public bool RememberUsername
    {
        get => _rememberUsername;
        set => Set(ref _rememberUsername, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => Set(ref _isBusy, value);
    }

    public bool IsNotBusy => !IsBusy;

    protected override void OnPropertyChanged(string? propertyName)
    {
        base.OnPropertyChanged(propertyName);

        switch (propertyName)
        {
            case nameof(Username):
            case nameof(Password):
            case nameof(IsBusy):
                if (propertyName == nameof(IsBusy))
                {
                    OnPropertyChanged(nameof(IsNotBusy));
                }

                _loginCommand.RaiseCanExecuteChanged();
                break;
        }
    }

    private bool CanLogin() =>
        !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Password)
        && !IsBusy;

    private async Task LoginAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            AuthenticationResult result = await _authenticationService.LoginAsync(Username, Password);
            if (!result.Succeeded)
            {
                ErrorMessage = result.ErrorMessage ?? "Не удалось выполнить вход.";
                return;
            }

            if (RememberUsername)
            {
                _configService.SetValue("Auth.LastUsername", Username.Trim());
            }
            else
            {
                _configService.RemoveValue("Auth.LastUsername");
            }

            _windowService.Close(this, true);
        }
        catch (Exception ex)
        {
            _windowService.ShowErrorMessage(ex.Message, "Ошибка входа");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

//⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⠀⡤⢤⣀⣤⣀⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⢀⣠⢶⠞⢩⣧⡨⠿⠿⢿⡝⠯⠛⠶⣄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⢀⣶⠟⠍⠁⢒⠿⡠⠖⠉⠉⢙⣷⠀⠀⠀⠈⠩⣲⣄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⢤⡿⣥⡖⣲⣿⣿⣞⣁⣀⠴⢚⣿⠛⣷⡈⣆⠀⠱⡌⠉⢧⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⢰⡿⢛⣶⣿⣿⣿⠋⣹⣟⣁⣴⣾⠃⢀⡏⠇⠸⡀⠀⢱⠀⢈⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⣿⡇⡘⣾⣿⣿⡇⣸⡯⠽⠟⢋⣉⠑⡞⠀⡼⢠⢧⠀⠀⡇⠈⢿⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠐⡿⢰⢁⡟⠀⠉⣰⠙⡿⣷⣶⢦⡄⢰⠁⢰⠃⣸⡌⠀⢸⠃⢀⢾⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⣷⢸⢸⢧⡰⢼⣿⡀⠉⠀⠈⠀⠀⠀⢧⢇⣸⣳⠁⡰⢃⠀⣸⣿⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⢿⣿⡸⣼⡝⢦⠣⠁⠀⠀⠀⠀⠀⠀⠘⠙⠻⢥⠞⢁⠜⣰⣿⣿⡿⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠈⢿⢿⣼⣇⠘⣧⡀⠀⠀⠀⠀⠀⠄⠀⠀⠀⠀⣼⣧⣾⡷⠛⢿⠓⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠸⠺⣿⣿⣇⣿⠙⢦⡀⠀⠀⠀⠀⠀⠀⢀⣼⡿⠋⠀⠀⠀⠈⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⢀⡤⠶⠶⠿⢿⣿⡇⠀⠀⠈⠓⠤⣤⡤⠖⠊⠉⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⡴⠋⠀⠀⠀⠀⠀⠙⠓⠤⠄⣀⡀⠀⢸⣷⣦⡤⠤⠖⠒⠒⠢⢤⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⢸⠃⠀⠀⠀⠀⠀⢀⢆⡀⠀⠂⠒⠒⠒⠻⠦⣄⡀⠀⢀⠢⠤⠤⢄⡹⣦⣀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⡚⠀⠀⠀⠀⠀⠀⡸⠋⠀⠀⠀⠀⠀⢀⠀⠀⠀⠈⠳⡄⠀⠀⠀⠀⠀⠈⠉⠳⣤⡀⠀⠀⠀⠀⠀⠀⠀⠀
//⢹⠀⠀⠀⠀⠀⢠⠇⠀⢀⠀⠀⠀⠀⠻⠇⠀⠀⠀⠀⠙⡄⠀⠀⠀⠀⠀⠀⠀⢬⣱⣄⠀⠀⠀⠀⠀⠀⠀
//⠀⣇⠀⠀⠀⠀⢸⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⣿⠀⠀⠀⠀⠀⠀⠀
//⠀⠻⡄⠀⠀⠀⠀⢇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣠⣇⠀⠀⠀⠀⠀⠀⠀⠀⠀⡿⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⣷⠀⠀⠀⠀⠈⡦⣀⠀⠀⠀⠀⠀⠀⠀⣀⠠⠖⠋⠈⠳⣄⠀⠀⠀⠀⠀⠀⢠⡟⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠹⡄⠀⠀⠀⠀⢸⠈⠉⠒⠒⠒⠊⠉⠁⠀⠀⠀⠀⠀⠀⠈⠳⣆⡀⠀⢀⡴⠟⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⢥⠀⠀⠀⠀⢸⡆⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠐⡿⠛⠉⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⢸⡄⠀⠀⠀⢸⣧⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⠀⠀⣧⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠄⠀⠀⠈⣷⠀⠀⠀⠀⡹⣿⡴⠏⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠘⠀⠀⠛⢧⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⣿⠄⠀⠀⠀⣿⠋⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠱⣄⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠭⠄⠀⠀⡰⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠻⡄⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠸⡆⠀⢰⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠓⢄⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⢻⣄⡎⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠢⡱⡄⠀⠀⠀
//⠀⠀⠀⠀⠀⠈⡿⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣀⠀⠀⠁⠙⡄⠀⠀
//⠀⠀⠀⠀⠀⢸⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⠃⠀⠀⠀⠀⠹⡄⠀
//⠀⠀⠀⠀⢀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠑⢦⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⠏⠀⠀⠀⠀⠀⠀⠹⠀
//⠀⠀⠀⠀⢸⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠢⡀⠀⠀⠀⠀⠀⠀⠀⡞⠀⠀⠀⠀⠀⠀⠀⠀⢳
//⠀⠀⠀⠀⢸⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠢⡀⠀⠀⠀⠀⢀⡇⠀⠀⠀⠀⠀⠀⠀⠀⢸
//⠀⠀⠀⠀⢸⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠹⡄⠀⠀⠀⢸⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈
//⠀⠀⠀⠀⠘⡆⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠘⡆⢀⣾⠏⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⢳⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢹⡏⠉⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠈⢧⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢳⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀


//⠄⠄⣿⣿⣿⣿⠘⡿⢛⣿⣿⣿⣿⣿⣧⢻⣿⣿⠃⠸⣿⣿⣿⠄⠄⠄⠄⠄
//⠄⠄⣿⣿⣿⣿⢀⠼⣛⣛⣭⢭⣟⣛⣛⣛⠿⠿⢆⡠⢿⣿⣿⠄⠄⠄⠄⠄
//⠄⠄⠸⣿⣿⢣⢶⣟⣿⣖⣿⣷⣻⣮⡿⣽⣿⣻⣖⣶⣤⣭⡉⠄⠄⠄⠄⠄
//⠄⠄⠄⢹⠣⣛⣣⣭⣭⣭⣁⡛⠻⢽⣿⣿⣿⣿⢻⣿⣿⣿⣽⡧⡄⠄⠄⠄
//⠄⠄⠄⠄⣼⣿⣿⣿⣿⣿⣿⣿⣿⣶⣌⡛⢿⣽⢘⣿⣷⣿⡻⠏⣛⣀⠄⠄
//⠄⠄⠄⣼⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣦⠙⡅⣿⠚⣡⣴⣿⣿⣿⡆⠄
//⠄⠄⣰⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⠄⣱⣾⣿⣿⣿⣿⣿⣿⠄
//⠄⢀⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⢸⣿⣿⣿⣿⣿⣿⣿⣿⠄
//⠄⣸⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡿⠣⣿⣿⣿⣿⣿⣿⣿⣿⣿⠄
//⠄⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⠿⠛⠑⣿⣮⣝⣛⠿⠿⣿⣿⣿⣿⠄
//⢠⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣶⠄⠄⠄⠄⣿⣿⣿⣿⣿⣿⣿⣿⣿⡟⠄

