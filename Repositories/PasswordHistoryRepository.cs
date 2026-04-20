using CargoTransport.Desktop.Data;
using CargoTransport.Desktop.Models;

namespace CargoTransport.Desktop.Repositories;

public interface IPasswordHistoryRepository
{
    void CreatePasswordHistory(PasswordHistory passwordHistory);
}

public sealed class PasswordHistoryRepository : RepositoryBase<PasswordHistory>, IPasswordHistoryRepository
{
    public PasswordHistoryRepository(CargoTransportDbContext context)
        : base(context)
    {
    }

    public void CreatePasswordHistory(PasswordHistory passwordHistory) => Create(passwordHistory);
}
