using CargoTransport.Desktop.Data;
using CargoTransport.Desktop.Repositories;
using CargoTransport.Desktop.Services;
using CargoTransport.Desktop.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sieve.Models;
using Sieve.Services;
using System.Windows;

namespace CargoTransport.Desktop;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private IServiceScope? _mainWindowScope;

    public ServiceProvider Services => _serviceProvider
        ?? throw new InvalidOperationException("Service provider is not initialized.");

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _serviceProvider = ConfigureServices();

        try
        {
            using IServiceScope startupScope = Services.CreateScope();
            await startupScope.ServiceProvider.GetRequiredService<IDatabaseInitializer>().InitializeAsync();
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

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        if (!ShowLoginAndMainWindow())
        {
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mainWindowScope?.Dispose();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private void HandleMainWindowClosed(object? sender, EventArgs e)
    {
        bool shouldReturnToLogin = !Services.GetRequiredService<IAuthStateService>().IsAuthenticated;

        _mainWindowScope?.Dispose();
        _mainWindowScope = null;
        MainWindow = null;

        if (shouldReturnToLogin)
        {
            if (!ShowLoginAndMainWindow())
            {
                Shutdown();
            }

            return;
        }

        Shutdown();
    }

    private bool ShowLoginAndMainWindow()
    {
        bool? dialogResult;
        using (IServiceScope loginScope = Services.CreateScope())
        {
            var loginWindow = loginScope.ServiceProvider.GetRequiredService<LoginWindow>();
            dialogResult = loginWindow.ShowDialog();
        }

        if (dialogResult != true)
        {
            return false;
        }

        _mainWindowScope = Services.CreateScope();
        MainWindow = _mainWindowScope.ServiceProvider.GetRequiredService<MainWindow>();
        MainWindow.Closed += HandleMainWindowClosed;
        MainWindow.Show();
        return true;
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IConfigService, ConfigService>();
        services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<IAuthStateService, AuthStateService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        services.AddTransient<IRepositoryManager, RepositoryManager>();
        services.AddTransient<IAuthenticationService, AuthenticationService>();
        services.AddTransient<IDatabaseInitializer, DatabaseInitializer>();
        services.AddTransient<IAdminPanelService, AdminPanelService>();
        services.AddTransient<IAdminCrudService, AdminCrudService>();
        services.AddTransient<IRoleOrderWorkspaceService, RoleOrderWorkspaceService>();
        services.AddTransient<IUserSelfService, UserSelfService>();
        services.Configure<SieveOptions>(options =>
        {
            options.CaseSensitive = false;
            options.ThrowExceptions = true;
        });
        services.AddScoped<ISieveCustomFilterMethods, CargoSieveFilters>();
        services.AddScoped<ISieveProcessor, CargoSieveProcessor>();

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
