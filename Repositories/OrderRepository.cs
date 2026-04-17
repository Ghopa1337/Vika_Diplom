using CargoTransport.Desktop.Data;
using CargoTransport.Desktop.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoTransport.Desktop.Repositories;

public interface IOrderRepository
{
    IQueryable<Order> GetAllOrdersDetailed(bool trackChanges);
    Task<Order?> GetOrderByIdDetailedAsync(uint id, bool trackChanges, CancellationToken cancellationToken = default);
    Task<bool> ExistsByOrderNumberExceptIdAsync(string orderNumber, uint orderId, CancellationToken cancellationToken = default);
    void CreateOrder(Order order);
    void UpdateOrder(Order order);
    void DeleteOrder(Order order);
}

public sealed class OrderRepository : RepositoryBase<Order>, IOrderRepository
{
    public OrderRepository(CargoTransportDbContext context)
        : base(context)
    {
    }

    public IQueryable<Order> GetAllOrdersDetailed(bool trackChanges) =>
        FindAll(trackChanges)
            .Include(x => x.ReceiverUser)
            .Include(x => x.Cargo)
            .Include(x => x.Driver)
                .ThenInclude(x => x!.User)
            .Include(x => x.Vehicle)
            .Include(x => x.CreatedByUser)
            .Include(x => x.StatusHistory)
                .ThenInclude(x => x.ChangedByUser);

    public Task<Order?> GetOrderByIdDetailedAsync(uint id, bool trackChanges, CancellationToken cancellationToken = default) =>
        GetAllOrdersDetailed(trackChanges)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByOrderNumberExceptIdAsync(string orderNumber, uint orderId, CancellationToken cancellationToken = default) =>
        FindByCondition(x => x.OrderNumber == orderNumber && x.Id != orderId, trackChanges: false)
            .AnyAsync(cancellationToken);

    public void CreateOrder(Order order) => Create(order);

    public void UpdateOrder(Order order) => Update(order);

    public void DeleteOrder(Order order) => Delete(order);
}
