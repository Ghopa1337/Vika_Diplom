using CargoTransport.Desktop.Data;
using CargoTransport.Desktop.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoTransport.Desktop.Repositories;

public interface IDriverRepository
{
    IQueryable<Driver> GetAllDriversWithUsers(bool trackChanges);
}

public sealed class DriverRepository : RepositoryBase<Driver>, IDriverRepository
{
    public DriverRepository(CargoTransportDbContext context)
        : base(context)
    {
    }

    public IQueryable<Driver> GetAllDriversWithUsers(bool trackChanges) =>
        FindAll(trackChanges)
            .Include(x => x.User);
}
