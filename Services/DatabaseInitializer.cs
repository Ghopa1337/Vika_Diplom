using CargoTransport.Desktop.Models;
using CargoTransport.Desktop.Repositories;

namespace CargoTransport.Desktop.Services;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IRepositoryManager _repositoryManager;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfigService _configService;

    public DatabaseInitializer(
        IRepositoryManager repositoryManager,
        IPasswordHasher passwordHasher,
        IConfigService configService)
    {
        _repositoryManager = repositoryManager;
        _passwordHasher = passwordHasher;
        _configService = configService;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!await _repositoryManager.Schema.CanConnectAsync(cancellationToken))
        {
            throw new InvalidOperationException("Не удалось подключиться к базе данных cargo_transport_db.");
        }

        await EnsureCoreTablesAsync(cancellationToken);
        await SeedRolesAsync(cancellationToken);
        await SeedDefaultAdministratorAsync(cancellationToken);
    }

    private async Task EnsureCoreTablesAsync(CancellationToken cancellationToken)
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

        const string createOrderRequestsTable = """
            CREATE TABLE IF NOT EXISTS order_requests (
                id INT UNSIGNED NOT NULL AUTO_INCREMENT,
                receiver_user_id INT UNSIGNED NOT NULL,
                cargo_description VARCHAR(500) NOT NULL,
                pickup_address VARCHAR(500) NOT NULL,
                delivery_address VARCHAR(500) NOT NULL,
                pickup_contact_phone VARCHAR(20) NOT NULL,
                delivery_contact_phone VARCHAR(20) NOT NULL,
                desired_date DATETIME NULL,
                comment TEXT NULL,
                status VARCHAR(30) NOT NULL DEFAULT 'pending',
                processed_by_user_id INT UNSIGNED NULL,
                created_order_id INT UNSIGNED NULL,
                processed_at DATETIME NULL,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                PRIMARY KEY (id),
                UNIQUE KEY uq_order_requests_created_order_id (created_order_id),
                KEY idx_order_requests_receiver_user_id (receiver_user_id),
                KEY idx_order_requests_status (status),
                KEY idx_order_requests_created_at (created_at),
                KEY idx_order_requests_processed_by_user_id (processed_by_user_id),
                CONSTRAINT fk_order_requests_receiver_user
                    FOREIGN KEY (receiver_user_id) REFERENCES users (id)
                    ON UPDATE CASCADE
                    ON DELETE RESTRICT,
                CONSTRAINT fk_order_requests_processed_by_user
                    FOREIGN KEY (processed_by_user_id) REFERENCES users (id)
                    ON UPDATE CASCADE
                    ON DELETE SET NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """;

        await _repositoryManager.Schema.ExecuteSqlAsync(createRolesTable, cancellationToken);
        await _repositoryManager.Schema.ExecuteSqlAsync(createUsersTable, cancellationToken);
        await _repositoryManager.Schema.ExecuteSqlAsync(createActivityLogsTable, cancellationToken);
        await _repositoryManager.Schema.ExecuteSqlAsync(createOrderRequestsTable, cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
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
            Role? role = await _repositoryManager.Role.GetRoleByCodeAsync(definition.Code, trackChanges: true, cancellationToken);

            if (role is null)
            {
                _repositoryManager.Role.CreateRole(new Role
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
                _repositoryManager.Role.UpdateRole(role);
            }
        }

        await _repositoryManager.SaveAsync(cancellationToken);
        _repositoryManager.Clear();
    }

    private async Task SeedDefaultAdministratorAsync(CancellationToken cancellationToken)
    {
        string username = _configService.GetValueOrDefault("App.DefaultAdminUsername", "admin");
        string password = _configService.GetValueOrDefault("App.DefaultAdminPassword", "Admin123!");
        string fullName = _configService.GetValueOrDefault("App.DefaultAdminFullName", "Системный администратор");

        bool userExists = await _repositoryManager.User.ExistsByUsernameAsync(username, cancellationToken);
        if (userExists)
        {
            return;
        }

        Role adminRole = await _repositoryManager.Role.GetRoleByCodeAsync("admin", trackChanges: false, cancellationToken)
            ?? throw new InvalidOperationException("Не найдена роль администратора.");

        _repositoryManager.User.CreateUser(new User
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

        await _repositoryManager.SaveAsync(cancellationToken);
        _repositoryManager.Clear();
    }
}
