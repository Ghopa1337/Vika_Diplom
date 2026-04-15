using CargoTransport.Desktop.Data;
using CargoTransport.Desktop.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoTransport.Desktop.Repositories;

public interface ICargoRepository
{
    IQueryable<CargoItem> GetAllCargo(bool trackChanges);
    Task<CargoItem?> GetCargoByIdAsync(uint id, bool trackChanges, CancellationToken cancellationToken = default);
    void CreateCargo(CargoItem cargo);
    void UpdateCargo(CargoItem cargo);
    void DeleteCargo(CargoItem cargo);
}

public sealed class CargoRepository : RepositoryBase<CargoItem>, ICargoRepository
{
    public CargoRepository(CargoTransportDbContext context)
        : base(context)
    {
    }

    public IQueryable<CargoItem> GetAllCargo(bool trackChanges) =>
        FindAll(trackChanges)
            .OrderBy(x => x.Name);

    public Task<CargoItem?> GetCargoByIdAsync(uint id, bool trackChanges, CancellationToken cancellationToken = default) =>
        FindByCondition(x => x.Id == id, trackChanges)
            .FirstOrDefaultAsync(cancellationToken);

    public void CreateCargo(CargoItem cargo) => Create(cargo);

    public void UpdateCargo(CargoItem cargo) => Update(cargo);

    public void DeleteCargo(CargoItem cargo) => Delete(cargo);
}
