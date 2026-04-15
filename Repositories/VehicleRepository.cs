using CargoTransport.Desktop.Data;
using CargoTransport.Desktop.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoTransport.Desktop.Repositories;

public interface IVehicleRepository
{
    IQueryable<Vehicle> GetAllVehiclesWithDrivers(bool trackChanges);
    Task<Vehicle?> GetVehicleByIdWithDriverAsync(uint id, bool trackChanges, CancellationToken cancellationToken = default);
    Task<bool> ExistsByLicensePlateExceptIdAsync(string licensePlate, uint vehicleId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCurrentDriverIdExceptIdAsync(uint currentDriverId, uint vehicleId, CancellationToken cancellationToken = default);
    void CreateVehicle(Vehicle vehicle);
    void UpdateVehicle(Vehicle vehicle);
    void DeleteVehicle(Vehicle vehicle);
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

    public Task<Vehicle?> GetVehicleByIdWithDriverAsync(uint id, bool trackChanges, CancellationToken cancellationToken = default) =>
        FindByCondition(x => x.Id == id, trackChanges)
            .Include(x => x.CurrentDriver)
                .ThenInclude(x => x!.User)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistsByLicensePlateExceptIdAsync(string licensePlate, uint vehicleId, CancellationToken cancellationToken = default) =>
        FindByCondition(x => x.LicensePlate == licensePlate && x.Id != vehicleId, trackChanges: false)
            .AnyAsync(cancellationToken);

    public Task<bool> ExistsByCurrentDriverIdExceptIdAsync(uint currentDriverId, uint vehicleId, CancellationToken cancellationToken = default) =>
        FindByCondition(x => x.CurrentDriverId == currentDriverId && x.Id != vehicleId, trackChanges: false)
            .AnyAsync(cancellationToken);

    public void CreateVehicle(Vehicle vehicle) => Create(vehicle);

    public void UpdateVehicle(Vehicle vehicle) => Update(vehicle);

    public void DeleteVehicle(Vehicle vehicle) => Delete(vehicle);
}
