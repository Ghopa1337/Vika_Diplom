using CargoTransport.Desktop.Data;
using CargoTransport.Desktop.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoTransport.Desktop.Repositories;

public interface IOrderRequestRepository
{
    IQueryable<OrderRequest> GetAllRequestsDetailed(bool trackChanges);
    IQueryable<OrderRequest> GetRequestsForReceiver(uint receiverUserId, bool trackChanges);
    IQueryable<OrderRequest> GetPendingRequestsDetailed(bool trackChanges);
    Task<OrderRequest?> GetRequestByIdDetailedAsync(uint id, bool trackChanges, CancellationToken cancellationToken = default);
    void CreateRequest(OrderRequest request);
    void UpdateRequest(OrderRequest request);
}

public sealed class OrderRequestRepository : RepositoryBase<OrderRequest>, IOrderRequestRepository
{
    public OrderRequestRepository(CargoTransportDbContext context)
        : base(context)
    {
    }

    public IQueryable<OrderRequest> GetAllRequestsDetailed(bool trackChanges) =>
        FindAll(trackChanges)
            .Include(x => x.ReceiverUser)
            .Include(x => x.ProcessedByUser)
            .Include(x => x.CreatedOrder);

    public IQueryable<OrderRequest> GetRequestsForReceiver(uint receiverUserId, bool trackChanges) =>
        GetAllRequestsDetailed(trackChanges)
            .Where(x => x.ReceiverUserId == receiverUserId);

    public IQueryable<OrderRequest> GetPendingRequestsDetailed(bool trackChanges) =>
        GetAllRequestsDetailed(trackChanges)
            .Where(x => x.Status == OrderRequest.OrderRequestStatuses.Pending);

    public Task<OrderRequest?> GetRequestByIdDetailedAsync(uint id, bool trackChanges, CancellationToken cancellationToken = default) =>
        GetAllRequestsDetailed(trackChanges)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public void CreateRequest(OrderRequest request) => Create(request);

    public void UpdateRequest(OrderRequest request) => Update(request);
}
