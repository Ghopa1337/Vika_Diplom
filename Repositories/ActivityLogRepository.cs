using CargoTransport.Desktop.Data;
using CargoTransport.Desktop.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoTransport.Desktop.Repositories;

public interface IActivityLogRepository
{
    IQueryable<ActivityLog> GetRecentActivityLogsWithUsers(int takeCount, bool trackChanges);
    void CreateActivityLog(ActivityLog activityLog);
}

public sealed class ActivityLogRepository : RepositoryBase<ActivityLog>, IActivityLogRepository
{
    public ActivityLogRepository(CargoTransportDbContext context)
        : base(context)
    {
    }

    public IQueryable<ActivityLog> GetRecentActivityLogsWithUsers(int takeCount, bool trackChanges) =>
        FindAll(trackChanges)
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
            .Take(takeCount);

    public void CreateActivityLog(ActivityLog activityLog) => Create(activityLog);
}
