namespace CargoTransport.Desktop.Services;

public interface IConnectionStringProvider
{
    string GetConnectionString();
}

public class ConnectionStringProvider : IConnectionStringProvider
{
    private readonly IConfigService _configService;

    public ConnectionStringProvider(IConfigService configService)
    {
        _configService = configService;
    }

    public string GetConnectionString()
    {
        string server = _configService.GetValueOrDefault("Database.Server", "127.0.0.1");
        string port = _configService.GetValueOrDefault("Database.Port", "3306");
        string database = _configService.GetValueOrDefault("Database.Name", "cargo_transport_db");
        string user = _configService.GetValueOrDefault("Database.User", "root");
        string password = _configService.GetValueOrDefault("Database.Password", "root");

        return $"Server={server};Port={port};Database={database};User={user};Password={password};CharSet=utf8mb4;SslMode=None;AllowUserVariables=True;";
    }
}
