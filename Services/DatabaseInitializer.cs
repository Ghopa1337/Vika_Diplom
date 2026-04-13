using CargoTransport.Desktop.Data;
using CargoTransport.Desktop.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoTransport.Desktop.Services;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IDbContextFactory<CargoTransportDbContext> _dbContextFactory;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfigService _configService;

    public DatabaseInitializer(
        IDbContextFactory<CargoTransportDbContext> dbContextFactory,
        IPasswordHasher passwordHasher,
        IConfigService configService)
    {
        _dbContextFactory = dbContextFactory;
        _passwordHasher = passwordHasher;
        _configService = configService;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using CargoTransportDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (!await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            throw new InvalidOperationException("Не удалось подключиться к базе данных cargo_transport_db.");
        }

        await EnsureCoreTablesAsync(dbContext, cancellationToken);
        await SeedRolesAsync(dbContext, cancellationToken);
        await SeedDefaultAdministratorAsync(dbContext, cancellationToken);
    }

    private static async Task EnsureCoreTablesAsync(CargoTransportDbContext dbContext, CancellationToken cancellationToken)
    {
        const string createRolesTable = """
            CREATE TABLE IF NOT EXISTS roles (
                id INT UNSIGNED NOT NULL AUTO_INCREMENT,
                code VARCHAR(50) NOT NULL,
                name VARCHAR(100) NOT NULL,
                description TEXT NULL,
                PRIMARY KEY (id),
                UNIQUE KEY uq_roles_code (code),
                UNIQUE KEY uq_roles_name (name)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """;

        const string createUsersTable = """
            CREATE TABLE IF NOT EXISTS users (
                id INT UNSIGNED NOT NULL AUTO_INCREMENT,
                username VARCHAR(100) NOT NULL,
                password_hash VARCHAR(255) NOT NULL,
                full_name VARCHAR(255) NOT NULL,
                role_id INT UNSIGNED NOT NULL,
                email VARCHAR(255) NULL,
                phone VARCHAR(20) NULL,
                company_name VARCHAR(255) NULL,
                is_blocked TINYINT(1) NOT NULL DEFAULT 0,
                is_active TINYINT(1) NOT NULL DEFAULT 1,
                must_change_password TINYINT(1) NOT NULL DEFAULT 0,
                last_login_at DATETIME NULL,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                PRIMARY KEY (id),
                UNIQUE KEY uq_users_username (username),
                UNIQUE KEY uq_users_email (email),
                KEY idx_users_role_id (role_id),
                CONSTRAINT fk_users_role
                    FOREIGN KEY (role_id) REFERENCES roles (id)
                    ON UPDATE CASCADE
                    ON DELETE RESTRICT
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """;

        const string createActivityLogsTable = """
            CREATE TABLE IF NOT EXISTS activity_logs (
                id INT UNSIGNED NOT NULL AUTO_INCREMENT,
                user_id INT UNSIGNED NULL,
                entity_type VARCHAR(50) NOT NULL,
                entity_id INT UNSIGNED NULL,
                action_code VARCHAR(100) NOT NULL,
                description TEXT NULL,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (id),
                KEY idx_activity_logs_user_id (user_id),
                KEY idx_activity_logs_entity (entity_type, entity_id),
                KEY idx_activity_logs_created_at (created_at),
                CONSTRAINT fk_activity_logs_user
                    FOREIGN KEY (user_id) REFERENCES users (id)
                    ON UPDATE CASCADE
                    ON DELETE SET NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(createRolesTable, cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(createUsersTable, cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(createActivityLogsTable, cancellationToken);
    }

    private static async Task SeedRolesAsync(CargoTransportDbContext dbContext, CancellationToken cancellationToken)
    {
        var definitions = new[]
        {
            new { Code = "admin", Name = "Администратор", Description = "Полный доступ к системе" },
            new { Code = "dispatcher", Name = "Диспетчер", Description = "Управление перевозками и заказами" },
            new { Code = "receiver", Name = "Получатель", Description = "Создание и отслеживание собственных заказов" },
            new { Code = "driver", Name = "Водитель", Description = "Исполнение перевозки и смена статусов" }
        };

        foreach (var definition in definitions)
        {
            Role? role = await dbContext.Roles.FirstOrDefaultAsync(x => x.Code == definition.Code, cancellationToken);

            if (role is null)
            {
                dbContext.Roles.Add(new Role
                {
                    Code = definition.Code,
                    Name = definition.Name,
                    Description = definition.Description
                });
            }
            else
            {
                role.Name = definition.Name;
                role.Description = definition.Description;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDefaultAdministratorAsync(CargoTransportDbContext dbContext, CancellationToken cancellationToken)
    {
        string username = _configService.GetValueOrDefault("App.DefaultAdminUsername", "admin");
        string password = _configService.GetValueOrDefault("App.DefaultAdminPassword", "Admin123!");
        string fullName = _configService.GetValueOrDefault("App.DefaultAdminFullName", "Системный администратор");

        bool userExists = await dbContext.Users.AnyAsync(x => x.Username == username, cancellationToken);
        if (userExists)
        {
            return;
        }

        Role adminRole = await dbContext.Roles.FirstAsync(x => x.Code == "admin", cancellationToken);

        dbContext.Users.Add(new User
        {
            Username = username,
            PasswordHash = _passwordHasher.HashPassword(password),
            FullName = fullName,
            RoleId = adminRole.Id,
            IsActive = true,
            IsBlocked = false,
            MustChangePassword = false,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
