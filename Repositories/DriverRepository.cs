using CargoTransport.Desktop.Data;
using CargoTransport.Desktop.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoTransport.Desktop.Repositories;

public interface IDriverRepository
{
    IQueryable<Driver> GetAllDriversWithUsers(bool trackChanges);
    Task<Driver?> GetDriverByIdWithUserAsync(uint id, bool trackChanges, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUserIdExceptIdAsync(uint userId, uint driverId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByLicenseNumberExceptIdAsync(string licenseNumber, uint driverId, CancellationToken cancellationToken = default);
    void CreateDriver(Driver driver);
    void UpdateDriver(Driver driver);
    void DeleteDriver(Driver driver);
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

    public Task<Driver?> GetDriverByIdWithUserAsync(uint id, bool trackChanges, CancellationToken cancellationToken = default) =>
        FindByCondition(x => x.Id == id, trackChanges)
            .Include(x => x.User)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistsByUserIdExceptIdAsync(uint userId, uint driverId, CancellationToken cancellationToken = default) =>
        FindByCondition(x => x.UserId == userId && x.Id != driverId, trackChanges: false)
            .AnyAsync(cancellationToken);

    public Task<bool> ExistsByLicenseNumberExceptIdAsync(string licenseNumber, uint driverId, CancellationToken cancellationToken = default) =>
        FindByCondition(x => x.LicenseNumber == licenseNumber && x.Id != driverId, trackChanges: false)
            .AnyAsync(cancellationToken);

    public void CreateDriver(Driver driver) => Create(driver);

    public void UpdateDriver(Driver driver) => Update(driver);

    public void DeleteDriver(Driver driver) => Delete(driver);
}
