using CargoTransport.Desktop.Data;
using CargoTransport.Desktop.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoTransport.Desktop.Repositories;

public interface IReportRepository
{
    IQueryable<Report> GetRecentReports(bool trackChanges);
}

public sealed class ReportRepository : RepositoryBase<Report>, IReportRepository
{
    public ReportRepository(CargoTransportDbContext context)
        : base(context)
    {
    }

    public IQueryable<Report> GetRecentReports(bool trackChanges) =>
        FindAll(trackChanges)
            .Include(x => x.CreatedByUser)
            .OrderByDescending(x => x.CreatedAt);
}
