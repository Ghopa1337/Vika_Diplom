using CargoTransport.Desktop.Models;
using CargoTransport.Desktop.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CargoTransport.Desktop.Services;

public sealed record UserProfileData(
    uint Id,
    string Username,
    string FullName,
    string RoleCode,
    string RoleName,
    string? Email,
    string? Phone,
    string? CompanyName,
    bool IsActive,
    bool IsBlocked,
    bool MustChangePassword,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? DriverLicenseNumber,
    string? DriverLicenseCategory,
    ushort? DriverExperienceYears,
    string? DriverStatus);

public sealed record UserProfileUpdateData(
    string FullName,
    string? Email,
    string? Phone,
    string? CompanyName);

public sealed record UserPasswordChangeData(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword);

public sealed record UserNotificationData(
    uint Id,
    string Title,
    string Message,
    string NotificationType,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt);

public interface IUserSelfService
{
    Task<UserProfileData> GetProfileAsync(CancellationToken cancellationToken = default);
    Task<UserProfileData> UpdateProfileAsync(UserProfileUpdateData data, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(UserPasswordChangeData data, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserNotificationData>> GetNotificationsAsync(bool unreadOnly, CancellationToken cancellationToken = default);
    Task<int> GetUnreadNotificationCountAsync(CancellationToken cancellationToken = default);
    Task MarkNotificationReadAsync(uint notificationId, CancellationToken cancellationToken = default);
    Task MarkAllNotificationsReadAsync(CancellationToken cancellationToken = default);
}

public sealed class UserSelfService : IUserSelfService
{
    private readonly IRepositoryManager _repositoryManager;
    private readonly IAuthStateService _authStateService;
    private readonly IPasswordHasher _passwordHasher;

    public UserSelfService(
        IRepositoryManager repositoryManager,
        IAuthStateService authStateService,
        IPasswordHasher passwordHasher)
    {
        _repositoryManager = repositoryManager;
        _authStateService = authStateService;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserProfileData> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        AuthenticatedUser currentUser = GetCurrentUser();
        User user = await GetUserAsync(currentUser.Id, trackChanges: false, cancellationToken);
        Driver? driver = await GetDriverProfileAsync(currentUser.Id, trackChanges: false, cancellationToken);

        _repositoryManager.Clear();
        return MapProfile(user, driver);
    }

    public async Task<UserProfileData> UpdateProfileAsync(UserProfileUpdateData data, CancellationToken cancellationToken = default)
    {
        AuthenticatedUser currentUser = GetCurrentUser();
        ValidateProfile(data);

        User user = await GetUserAsync(currentUser.Id, trackChanges: true, cancellationToken);
        string? email = NormalizeOptional(data.Email);

        if (!string.IsNullOrWhiteSpace(email)
            && await _repositoryManager.User.ExistsByEmailExceptIdAsync(email, user.Id, cancellationToken))
        {
            throw new InvalidOperationException("Пользователь с такой почтой уже существует.");
        }

        user.FullName = data.FullName.Trim();
        user.Email = email;
        user.Phone = NormalizeOptional(data.Phone);
        user.CompanyName = NormalizeOptional(data.CompanyName);
        user.UpdatedAt = DateTime.Now;

        _repositoryManager.User.UpdateUser(user);
        _repositoryManager.ActivityLog.CreateActivityLog(new ActivityLog
        {
            UserId = user.Id,
            EntityType = "user",
            EntityId = user.Id,
            ActionCode = "profile_updated",
            Description = $"Пользователь {user.Username} обновил профиль",
            CreatedAt = DateTime.Now
        });

        await _repositoryManager.SaveAsync(cancellationToken);

        _authStateService.SignIn(new AuthenticatedUser(
            user.Id,
            user.Username,
            user.FullName,
            user.Role.Code,
            user.Role.Name));

        Driver? driver = await GetDriverProfileAsync(user.Id, trackChanges: false, cancellationToken);
        UserProfileData profile = MapProfile(user, driver);
        _repositoryManager.Clear();
        return profile;
    }

    public async Task ChangePasswordAsync(UserPasswordChangeData data, CancellationToken cancellationToken = default)
    {
        AuthenticatedUser currentUser = GetCurrentUser();
        ValidatePasswordChange(data);

        User user = await GetUserAsync(currentUser.Id, trackChanges: true, cancellationToken);

        if (!_passwordHasher.VerifyPassword(data.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Текущий пароль указан неверно.");
        }

        if (_passwordHasher.VerifyPassword(data.NewPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Новый пароль должен отличаться от текущего.");
        }

        DateTime now = DateTime.Now;
        _repositoryManager.PasswordHistory.CreatePasswordHistory(new PasswordHistory
        {
            UserId = user.Id,
            OldPasswordHash = user.PasswordHash,
            ChangedAt = now
        });

        user.PasswordHash = _passwordHasher.HashPassword(data.NewPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = now;

        _repositoryManager.User.UpdateUser(user);
        _repositoryManager.Notification.CreateNotification(new Notification
        {
            UserId = user.Id,
            Title = "Пароль изменен",
            Message = "Пароль учетной записи был успешно обновлен.",
            NotificationType = "security",
            IsRead = false,
            CreatedAt = now
        });
        _repositoryManager.ActivityLog.CreateActivityLog(new ActivityLog
        {
            UserId = user.Id,
            EntityType = "user",
            EntityId = user.Id,
            ActionCode = "password_changed",
            Description = $"Пользователь {user.Username} сменил пароль",
            CreatedAt = now
        });

        await _repositoryManager.SaveAsync(cancellationToken);
        _repositoryManager.Clear();
    }

    public async Task<IReadOnlyList<UserNotificationData>> GetNotificationsAsync(bool unreadOnly, CancellationToken cancellationToken = default)
    {
        AuthenticatedUser currentUser = GetCurrentUser();
        IQueryable<Notification> query = _repositoryManager.Notification.GetNotificationsForUser(currentUser.Id, trackChanges: false);

        if (unreadOnly)
        {
            query = query.Where(x => !x.IsRead);
        }

        List<UserNotificationData> notifications = await query
            .Take(100)
            .Select(x => new UserNotificationData(
                x.Id,
                x.Title,
                x.Message,
                x.NotificationType,
                x.IsRead,
                x.CreatedAt,
                x.ReadAt))
            .ToListAsync(cancellationToken);

        _repositoryManager.Clear();
        return notifications;
    }

    public async Task<int> GetUnreadNotificationCountAsync(CancellationToken cancellationToken = default)
    {
        AuthenticatedUser currentUser = GetCurrentUser();
        int count = await _repositoryManager.Notification
            .GetNotificationsForUser(currentUser.Id, trackChanges: false)
            .CountAsync(x => !x.IsRead, cancellationToken);

        _repositoryManager.Clear();
        return count;
    }

    public async Task MarkNotificationReadAsync(uint notificationId, CancellationToken cancellationToken = default)
    {
        AuthenticatedUser currentUser = GetCurrentUser();
        Notification notification = await _repositoryManager.Notification.GetNotificationForUserAsync(notificationId, currentUser.Id, trackChanges: true, cancellationToken)
            ?? throw new InvalidOperationException("Уведомление не найдено.");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.Now;
            _repositoryManager.Notification.UpdateNotification(notification);
            await _repositoryManager.SaveAsync(cancellationToken);
        }

        _repositoryManager.Clear();
    }

    public async Task MarkAllNotificationsReadAsync(CancellationToken cancellationToken = default)
    {
        AuthenticatedUser currentUser = GetCurrentUser();
        List<Notification> notifications = await _repositoryManager.Notification
            .GetNotificationsForUser(currentUser.Id, trackChanges: true)
            .Where(x => !x.IsRead)
            .ToListAsync(cancellationToken);

        DateTime now = DateTime.Now;
        foreach (Notification notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
            _repositoryManager.Notification.UpdateNotification(notification);
        }

        if (notifications.Count > 0)
        {
            await _repositoryManager.SaveAsync(cancellationToken);
        }

        _repositoryManager.Clear();
    }

    private async Task<User> GetUserAsync(uint userId, bool trackChanges, CancellationToken cancellationToken) =>
        await _repositoryManager.User.GetUserByIdWithRoleAsync(userId, trackChanges, cancellationToken)
        ?? throw new InvalidOperationException("Текущий пользователь не найден.");

    private async Task<Driver?> GetDriverProfileAsync(uint userId, bool trackChanges, CancellationToken cancellationToken) =>
        await _repositoryManager.Driver.GetAllDriversWithUsers(trackChanges)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    private static UserProfileData MapProfile(User user, Driver? driver) =>
        new(
            user.Id,
            user.Username,
            user.FullName,
            user.Role.Code,
            user.Role.Name,
            user.Email,
            user.Phone,
            user.CompanyName,
            user.IsActive,
            user.IsBlocked,
            user.MustChangePassword,
            user.LastLoginAt,
            user.CreatedAt,
            user.UpdatedAt,
            driver?.LicenseNumber,
            driver?.LicenseCategory,
            driver?.ExperienceYears,
            driver?.Status);

    private static void ValidateProfile(UserProfileUpdateData data)
    {
        if (string.IsNullOrWhiteSpace(data.FullName))
        {
            throw new InvalidOperationException("Укажите ФИО или название контактного лица.");
        }

        if (!string.IsNullOrWhiteSpace(data.Email) && !data.Email.Contains('@', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Email должен содержать символ @.");
        }
    }

    private static void ValidatePasswordChange(UserPasswordChangeData data)
    {
        if (string.IsNullOrWhiteSpace(data.CurrentPassword))
        {
            throw new InvalidOperationException("Введите текущий пароль.");
        }

        if (string.IsNullOrWhiteSpace(data.NewPassword) || data.NewPassword.Length < 6)
        {
            throw new InvalidOperationException("Новый пароль должен содержать минимум 6 символов.");
        }

        if (!string.Equals(data.NewPassword, data.ConfirmPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Подтверждение пароля не совпадает.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private AuthenticatedUser GetCurrentUser() =>
        _authStateService.CurrentUser
        ?? throw new InvalidOperationException("Не удалось определить текущего пользователя.");
}
