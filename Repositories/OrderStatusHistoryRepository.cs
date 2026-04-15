using CargoTransport.Desktop.Data;
using CargoTransport.Desktop.Models;

namespace CargoTransport.Desktop.Repositories;

public interface IOrderStatusHistoryRepository
{
    void CreateOrderStatusHistory(OrderStatusHistory statusHistory);
}

public sealed class OrderStatusHistoryRepository : RepositoryBase<OrderStatusHistory>, IOrderStatusHistoryRepository
{
    public OrderStatusHistoryRepository(CargoTransportDbContext context)
        : base(context)
    {
    }

    public void CreateOrderStatusHistory(OrderStatusHistory statusHistory) => Create(statusHistory);
}
