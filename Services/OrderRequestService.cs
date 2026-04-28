using CargoTransport.Desktop.Models;
using CargoTransport.Desktop.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CargoTransport.Desktop.Services;

public sealed record OrderRequestDraftData(
    string CargoDescription,
    string PickupAddress,
    string DeliveryAddress,
    string PickupContactPhone,
    string DeliveryContactPhone,
    DateTime? DesiredDate,
    string? Comment);

public interface IOrderRequestService
{
    Task<IReadOnlyList<OrderRequest>> GetRequestsForCurrentReceiverAsync(CancellationToken cancellationToken = default);
    Task<OrderRequest?> GetRequestByIdAsync(uint requestId, CancellationToken cancellationToken = default);
    Task<OrderRequest> CreateRequestForCurrentReceiverAsync(OrderRequestDraftData data, CancellationToken cancellationToken = default);
    Task MarkRequestProcessedAsync(uint requestId, uint createdOrderId, CancellationToken cancellationToken = default);
}

public sealed class OrderRequestService : IOrderRequestService
{
    private readonly IRepositoryManager _repositoryManager;
    private readonly IAuthStateService _authStateService;

    public OrderRequestService(
        IRepositoryManager repositoryManager,
        IAuthStateService authStateService)
    {
        _repositoryManager = repositoryManager;
        _authStateService = authStateService;
    }

    public async Task<IReadOnlyList<OrderRequest>> GetRequestsForCurrentReceiverAsync(CancellationToken cancellationToken = default)
    {
        AuthenticatedUser currentUser = GetCurrentUser();

        List<OrderRequest> requests = await _repositoryManager.OrderRequest
            .GetRequestsForReceiver(currentUser.Id, trackChanges: false)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        _repositoryManager.Clear();
        return requests;
    }

    public async Task<OrderRequest?> GetRequestByIdAsync(uint requestId, CancellationToken cancellationToken = default)
    {
        OrderRequest? request = await _repositoryManager.OrderRequest
            .GetRequestByIdDetailedAsync(requestId, trackChanges: false, cancellationToken);

        _repositoryManager.Clear();
        return request;
    }

    public async Task<OrderRequest> CreateRequestForCurrentReceiverAsync(OrderRequestDraftData data, CancellationToken cancellationToken = default)
    {
        ValidateDraft(data);

        AuthenticatedUser currentUser = GetCurrentUser();
        DateTime now = DateTime.Now;
        var request = new OrderRequest
        {
            ReceiverUserId = currentUser.Id,
            CargoDescription = data.CargoDescription.Trim(),
            PickupAddress = data.PickupAddress.Trim(),
            DeliveryAddress = data.DeliveryAddress.Trim(),
            PickupContactPhone = InputValidationHelper.KeepDigitsOnly(data.PickupContactPhone),
            DeliveryContactPhone = InputValidationHelper.KeepDigitsOnly(data.DeliveryContactPhone),
            DesiredDate = data.DesiredDate,
            Comment = NormalizeOptional(data.Comment),
            Status = OrderRequest.OrderRequestStatuses.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        _repositoryManager.OrderRequest.CreateRequest(request);
        await _repositoryManager.SaveAsync(cancellationToken);

        await CreateDispatcherNotificationsAsync(request, cancellationToken);
        await LogAsync(
            "order_request",
            request.Id,
            "order_request_created",
            $"Создана заявка #{request.Id} от получателя {currentUser.FullName}",
            cancellationToken);

        return request;
    }

    public async Task MarkRequestProcessedAsync(uint requestId, uint createdOrderId, CancellationToken cancellationToken = default)
    {
        AuthenticatedUser currentUser = GetCurrentUser();
        OrderRequest request = await _repositoryManager.OrderRequest
            .GetRequestByIdDetailedAsync(requestId, trackChanges: true, cancellationToken)
            ?? throw new InvalidOperationException("Заявка не найдена.");

        if (request.Status != OrderRequest.OrderRequestStatuses.Pending)
        {
            throw new InvalidOperationException("Заявка уже обработана.");
        }

        Order order = await _repositoryManager.Order.GetOrderByIdDetailedAsync(createdOrderId, trackChanges: false, cancellationToken)
            ?? throw new InvalidOperationException("Созданный заказ не найден.");

        DateTime now = DateTime.Now;
        request.Status = OrderRequest.OrderRequestStatuses.Processed;
        request.CreatedOrderId = createdOrderId;
        request.ProcessedByUserId = currentUser.Id;
        request.ProcessedAt = now;
        request.UpdatedAt = now;

        _repositoryManager.OrderRequest.UpdateRequest(request);
        _repositoryManager.Notification.CreateNotification(new Notification
        {
            UserId = request.ReceiverUserId,
            Title = $"По заявке #{request.Id} создан заказ {order.OrderNumber}",
            Message = "Диспетчер оформил полноценный заказ по вашей заявке. Проверьте раздел с заказами.",
            NotificationType = "order_request",
            IsRead = false,
            CreatedAt = now
        });

        await _repositoryManager.SaveAsync(cancellationToken);
        await LogAsync(
            "order_request",
            request.Id,
            "order_request_processed",
            $"По заявке #{request.Id} создан заказ {order.OrderNumber}",
            cancellationToken);
    }

    private async Task CreateDispatcherNotificationsAsync(OrderRequest request, CancellationToken cancellationToken)
    {
        DateTime now = DateTime.Now;
        List<User> dispatchers = await _repositoryManager.User
            .GetUsersByRoleCode("dispatcher", trackChanges: false)
            .Where(x => x.IsActive && !x.IsBlocked)
            .ToListAsync(cancellationToken);

        foreach (User dispatcher in dispatchers)
        {
            _repositoryManager.Notification.CreateNotification(new Notification
            {
                UserId = dispatcher.Id,
                Title = $"Новая заявка #{request.Id}",
                Message = "Получена новая заявка от получателя. Откройте раздел заказов, чтобы оформить перевозку.",
                NotificationType = "order_request",
                IsRead = false,
                CreatedAt = now
            });
        }

        if (dispatchers.Count > 0)
        {
            await _repositoryManager.SaveAsync(cancellationToken);
        }
    }

    private async Task LogAsync(string entityType, uint entityId, string actionCode, string description, CancellationToken cancellationToken)
    {
        _repositoryManager.ActivityLog.CreateActivityLog(new ActivityLog
        {
            UserId = _authStateService.CurrentUser?.Id,
            EntityType = entityType,
            EntityId = entityId,
            ActionCode = actionCode,
            Description = description,
            CreatedAt = DateTime.Now
        });

        await _repositoryManager.SaveAsync(cancellationToken);
        _repositoryManager.Clear();
    }

    private AuthenticatedUser GetCurrentUser() =>
        _authStateService.CurrentUser
        ?? throw new InvalidOperationException("Текущий пользователь не определен.");

    private static void ValidateDraft(OrderRequestDraftData data)
    {
        if (string.IsNullOrWhiteSpace(data.CargoDescription))
        {
            throw new InvalidOperationException("Укажите груз в заявке.");
        }

        if (string.IsNullOrWhiteSpace(data.PickupAddress))
        {
            throw new InvalidOperationException("Укажите адрес отправления.");
        }

        if (string.IsNullOrWhiteSpace(data.DeliveryAddress))
        {
            throw new InvalidOperationException("Укажите адрес доставки.");
        }

        if (string.IsNullOrWhiteSpace(data.PickupContactPhone))
        {
            throw new InvalidOperationException("Укажите телефон на погрузке.");
        }

        if (string.IsNullOrWhiteSpace(data.DeliveryContactPhone))
        {
            throw new InvalidOperationException("Укажите телефон получателя.");
        }

        if (data.DesiredDate.HasValue && data.DesiredDate.Value.Date < DateTime.Today)
        {
            throw new InvalidOperationException("Дата заявки не может быть раньше сегодняшнего дня.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
