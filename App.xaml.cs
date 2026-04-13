using CargoTransport.Desktop.Data;
using CargoTransport.Desktop.Services;
using CargoTransport.Desktop.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace CargoTransport.Desktop;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public IServiceProvider Services => _serviceProvider
        ?? throw new InvalidOperationException("Service provider is not initialized.");

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _serviceProvider = ConfigureServices();

        try
        {
            await Services.GetRequiredService<IDatabaseInitializer>().InitializeAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Не удалось подключиться к MariaDB.\n\n{ex.Message}\n\nПроверь настройки в App.config и доступность локального сервера базы данных.",
                "Ошибка подключения к БД",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown();
            return;
        }

        var loginWindow = Services.GetRequiredService<LoginWindow>();
        bool? dialogResult = loginWindow.ShowDialog();

        if (dialogResult == true)
        {
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            MainWindow = Services.GetRequiredService<MainWindow>();
            MainWindow.Show();
            return;
        }

        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IConfigService, ConfigService>();
        services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<IAuthStateService, AuthStateService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();

        services.AddDbContextFactory<CargoTransportDbContext>((provider, options) =>
        {
            string connectionString = provider.GetRequiredService<IConnectionStringProvider>().GetConnectionString();
            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString),
                builder => builder.EnableRetryOnFailure(3));
        });

        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();

        return services.BuildServiceProvider();
    }
}
