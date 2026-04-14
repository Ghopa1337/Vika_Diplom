using CargoTransport.Desktop.Data;
using CargoTransport.Desktop.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoTransport.Desktop.Repositories;

public interface IVehicleRepository
{
    IQueryable<Vehicle> GetAllVehiclesWithDrivers(bool trackChanges);
}

public sealed class VehicleRepository : RepositoryBase<Vehicle>, IVehicleRepository
{
    public VehicleRepository(CargoTransportDbContext context)
        : base(context)
    {
    }

    public IQueryable<Vehicle> GetAllVehiclesWithDrivers(bool trackChanges) =>
        FindAll(trackChanges)
            .Include(x => x.CurrentDriver)
                .ThenInclude(x => x!.User);
}
