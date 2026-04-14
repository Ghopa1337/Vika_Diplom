using CargoTransport.Desktop.Data;
using Microsoft.EntityFrameworkCore;

namespace CargoTransport.Desktop.Repositories;

public interface ISchemaRepository
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
    Task ExecuteSqlAsync(string sql, CancellationToken cancellationToken = default);
}

public sealed class SchemaRepository : ISchemaRepository
{
    private readonly CargoTransportDbContext _context;

    public SchemaRepository(CargoTransportDbContext context)
    {
        _context = context;
    }

    public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default) =>
        _context.Database.CanConnectAsync(cancellationToken);

    public Task ExecuteSqlAsync(string sql, CancellationToken cancellationToken = default) =>
        _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
}
