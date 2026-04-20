using CargoTransport.Desktop.Data;
using CargoTransport.Desktop.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoTransport.Desktop.Repositories;

public interface INotificationRepository
{
    IQueryable<Notification> GetNotificationsForUser(uint userId, bool trackChanges);
    Task<Notification?> GetNotificationForUserAsync(uint notificationId, uint userId, bool trackChanges, CancellationToken cancellationToken = default);
    void CreateNotification(Notification notification);
    void UpdateNotification(Notification notification);
}

public sealed class NotificationRepository : RepositoryBase<Notification>, INotificationRepository
{
    public NotificationRepository(CargoTransportDbContext context)
        : base(context)
    {
    }

    public IQueryable<Notification> GetNotificationsForUser(uint userId, bool trackChanges) =>
        FindByCondition(x => x.UserId == userId, trackChanges)
            .OrderByDescending(x => x.CreatedAt);

    public Task<Notification?> GetNotificationForUserAsync(uint notificationId, uint userId, bool trackChanges, CancellationToken cancellationToken = default) =>
        FindByCondition(x => x.Id == notificationId && x.UserId == userId, trackChanges)
            .FirstOrDefaultAsync(cancellationToken);

    public void CreateNotification(Notification notification) => Create(notification);

    public void UpdateNotification(Notification notification) => Update(notification);
}
