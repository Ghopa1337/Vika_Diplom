using CargoTransport.Desktop.Data;
using CargoTransport.Desktop.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoTransport.Desktop.Repositories;

public interface IUserRepository
{
    IQueryable<User> GetAllUsersWithRoles(bool trackChanges);
    IQueryable<User> GetUsersByRoleCode(string roleCode, bool trackChanges);
    Task<User?> GetUserByUsernameAsync(string username, bool trackChanges, CancellationToken cancellationToken = default);
    Task<User?> GetUserByUsernameWithRoleAsync(string username, bool trackChanges, CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdWithRoleAsync(uint id, bool trackChanges, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUsernameExceptIdAsync(string username, uint userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailExceptIdAsync(string email, uint userId, CancellationToken cancellationToken = default);
    void CreateUser(User user);
    void UpdateUser(User user);
    void DeleteUser(User user);
}

public sealed class UserRepository : RepositoryBase<User>, IUserRepository
{
    public UserRepository(CargoTransportDbContext context)
        : base(context)
    {
    }

    public IQueryable<User> GetAllUsersWithRoles(bool trackChanges) =>
        FindAll(trackChanges)
            .Include(x => x.Role);

    public IQueryable<User> GetUsersByRoleCode(string roleCode, bool trackChanges) =>
        FindByCondition(x => x.Role.Code == roleCode, trackChanges)
            .Include(x => x.Role);

    public Task<User?> GetUserByUsernameAsync(string username, bool trackChanges, CancellationToken cancellationToken = default) =>
        FindByCondition(x => x.Username == username, trackChanges)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<User?> GetUserByUsernameWithRoleAsync(string username, bool trackChanges, CancellationToken cancellationToken = default) =>
        FindByCondition(x => x.Username == username, trackChanges)
            .Include(x => x.Role)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<User?> GetUserByIdWithRoleAsync(uint id, bool trackChanges, CancellationToken cancellationToken = default) =>
        FindByCondition(x => x.Id == id, trackChanges)
            .Include(x => x.Role)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        FindByCondition(x => x.Username == username, trackChanges: false)
            .AnyAsync(cancellationToken);

    public Task<bool> ExistsByUsernameExceptIdAsync(string username, uint userId, CancellationToken cancellationToken = default) =>
        FindByCondition(x => x.Username == username && x.Id != userId, trackChanges: false)
            .AnyAsync(cancellationToken);

    public Task<bool> ExistsByEmailExceptIdAsync(string email, uint userId, CancellationToken cancellationToken = default) =>
        FindByCondition(x => x.Email == email && x.Id != userId, trackChanges: false)
            .AnyAsync(cancellationToken);

    public void CreateUser(User user) => Create(user);

    public void UpdateUser(User user) => Update(user);

    public void DeleteUser(User user) => Delete(user);
}
