using CargoTransport.Desktop.Data;
using CargoTransport.Desktop.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoTransport.Desktop.Repositories;

public interface IRoleRepository
{
    IQueryable<Role> GetAllRoles(bool trackChanges);
    Task<Role?> GetRoleByCodeAsync(string code, bool trackChanges, CancellationToken cancellationToken = default);
    void CreateRole(Role role);
    void UpdateRole(Role role);
}

public sealed class RoleRepository : RepositoryBase<Role>, IRoleRepository
{
    public RoleRepository(CargoTransportDbContext context)
        : base(context)
    {
    }

    public IQueryable<Role> GetAllRoles(bool trackChanges) => FindAll(trackChanges);

    public Task<Role?> GetRoleByCodeAsync(string code, bool trackChanges, CancellationToken cancellationToken = default) =>
        FindByCondition(x => x.Code == code, trackChanges)
            .FirstOrDefaultAsync(cancellationToken);

    public void CreateRole(Role role) => Create(role);

    public void UpdateRole(Role role) => Update(role);
}
