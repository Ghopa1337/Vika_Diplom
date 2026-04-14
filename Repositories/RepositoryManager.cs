using CargoTransport.Desktop.Data;
using Microsoft.EntityFrameworkCore;

namespace CargoTransport.Desktop.Repositories;

public interface IRepositoryManager : IAsyncDisposable, IDisposable
{
    ISchemaRepository Schema { get; }
    IRoleRepository Role { get; }
    IUserRepository User { get; }
    IDriverRepository Driver { get; }
    IVehicleRepository Vehicle { get; }
    IOrderRepository Order { get; }
    IActivityLogRepository ActivityLog { get; }
    IReportRepository Report { get; }
    Task SaveAsync(CancellationToken cancellationToken = default);
    void Clear();
}

public sealed class RepositoryManager : IRepositoryManager
{
    private readonly CargoTransportDbContext _context;
    private ISchemaRepository? _schemaRepository;
    private IRoleRepository? _roleRepository;
    private IUserRepository? _userRepository;
    private IDriverRepository? _driverRepository;
    private IVehicleRepository? _vehicleRepository;
    private IOrderRepository? _orderRepository;
    private IActivityLogRepository? _activityLogRepository;
    private IReportRepository? _reportRepository;

    public RepositoryManager(IDbContextFactory<CargoTransportDbContext> dbContextFactory)
    {
        _context = dbContextFactory.CreateDbContext();
    }

    public ISchemaRepository Schema => _schemaRepository ??= new SchemaRepository(_context);
    public IRoleRepository Role => _roleRepository ??= new RoleRepository(_context);
    public IUserRepository User => _userRepository ??= new UserRepository(_context);
    public IDriverRepository Driver => _driverRepository ??= new DriverRepository(_context);
    public IVehicleRepository Vehicle => _vehicleRepository ??= new VehicleRepository(_context);
    public IOrderRepository Order => _orderRepository ??= new OrderRepository(_context);
    public IActivityLogRepository ActivityLog => _activityLogRepository ??= new ActivityLogRepository(_context);
    public IReportRepository Report => _reportRepository ??= new ReportRepository(_context);

    public Task SaveAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public void Clear() => _context.ChangeTracker.Clear();

    public void Dispose() => _context.Dispose();

    public ValueTask DisposeAsync() => _context.DisposeAsync();
}
