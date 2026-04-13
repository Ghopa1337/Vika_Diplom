using CargoTransport.Desktop.Data;
using CargoTransport.Desktop.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoTransport.Desktop.Services;

public sealed record AuthenticationResult(bool Succeeded, string? ErrorMessage, AuthenticatedUser? User);

public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly IDbContextFactory<CargoTransportDbContext> _dbContextFactory;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthStateService _authStateService;

    public AuthenticationService(
        IDbContextFactory<CargoTransportDbContext> dbContextFactory,
        IPasswordHasher passwordHasher,
        IAuthStateService authStateService)
    {
        _dbContextFactory = dbContextFactory;
        _passwordHasher = passwordHasher;
        _authStateService = authStateService;
    }

    public async Task<AuthenticationResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return new AuthenticationResult(false, "Введите логин и пароль.", null);
        }

        await using CargoTransportDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        User? user = await dbContext.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Username == username.Trim(), cancellationToken);

        if (user is null)
        {
            return new AuthenticationResult(false, "Пользователь не найден.", null);
        }

        if (!user.IsActive || user.IsBlocked)
        {
            return new AuthenticationResult(false, "Учетная запись недоступна.", null);
        }

        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            return new AuthenticationResult(false, "Неверный пароль.", null);
        }

        user.LastLoginAt = DateTime.Now;

        dbContext.ActivityLogs.Add(new ActivityLog
        {
            UserId = user.Id,
            EntityType = "user",
            EntityId = user.Id,
            ActionCode = "login",
            Description = $"{user.FullName} выполнил вход в систему",
            CreatedAt = DateTime.Now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var authenticatedUser = new AuthenticatedUser(
            user.Id,
            user.Username,
            user.FullName,
            user.Role.Code,
            user.Role.Name);

        _authStateService.SignIn(authenticatedUser);

        return new AuthenticationResult(true, null, authenticatedUser);
    }
}
