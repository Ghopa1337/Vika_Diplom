using CargoTransport.Desktop.Models;
using CargoTransport.Desktop.Repositories;

namespace CargoTransport.Desktop.Services;

public sealed record AuthenticationResult(bool Succeeded, string? ErrorMessage, AuthenticatedUser? User);

public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
}

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IRepositoryManager _repositoryManager;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthStateService _authStateService;

    public AuthenticationService(
        IRepositoryManager repositoryManager,
        IPasswordHasher passwordHasher,
        IAuthStateService authStateService)
    {
        _repositoryManager = repositoryManager;
        _passwordHasher = passwordHasher;
        _authStateService = authStateService;
    }

    public async Task<AuthenticationResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return new AuthenticationResult(false, "Введите логин и пароль.", null);
        }

        User? user = await _repositoryManager.User
            .GetUserByUsernameWithRoleAsync(username.Trim(), trackChanges: true, cancellationToken);

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
        _repositoryManager.User.UpdateUser(user);

        _repositoryManager.ActivityLog.CreateActivityLog(new ActivityLog
        {
            UserId = user.Id,
            EntityType = "user",
            EntityId = user.Id,
            ActionCode = "login",
            Description = $"{user.FullName} выполнил вход в систему",
            CreatedAt = DateTime.Now
        });

        await _repositoryManager.SaveAsync(cancellationToken);
        _repositoryManager.Clear();

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
