using CargoTransport.Desktop.Data;
using CargoTransport.Desktop.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoTransport.Desktop.Repositories;

public interface IOrderRepository
{
    IQueryable<Order> GetAllOrdersDetailed(bool trackChanges);
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
            .Include(x => x.CreatedByUser);
}
