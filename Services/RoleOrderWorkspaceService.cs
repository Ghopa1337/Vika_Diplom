using CargoTransport.Desktop.Models;
using CargoTransport.Desktop.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CargoTransport.Desktop.Services;

public enum RoleOrderCabinetMode
{
    Receiver,
    Driver
}

public enum RoleOrderAction
{
    Accept,
    Refuse,
    StartLoading,
    MarkDelivered,
    ConfirmReceipt
}

public interface IRoleOrderWorkspaceService
{
    Task<IReadOnlyList<Order>> GetOrdersForCurrentUserAsync(RoleOrderCabinetMode mode, CancellationToken cancellationToken = default);
    Task ExecuteActionAsync(RoleOrderCabinetMode mode, uint orderId, RoleOrderAction action, CancellationToken cancellationToken = default);
}

public sealed class RoleOrderWorkspaceService : IRoleOrderWorkspaceService
{
    private readonly IRepositoryManager _repositoryManager;
    private readonly IAuthStateService _authStateService;

    public RoleOrderWorkspaceService(
        IRepositoryManager repositoryManager,
        IAuthStateService authStateService)
    {
        _repositoryManager = repositoryManager;
        _authStateService = authStateService;
    }

    public async Task<IReadOnlyList<Order>> GetOrdersForCurrentUserAsync(RoleOrderCabinetMode mode, CancellationToken cancellationToken = default)
    {
        AuthenticatedUser currentUser = GetCurrentUser();

        IQueryable<Order> query = _repositoryManager.Order.GetAllOrdersDetailed(trackChanges: false);
        query = mode switch
        {
            RoleOrderCabinetMode.Receiver => query.Where(x => x.ReceiverUserId == currentUser.Id),
            RoleOrderCabinetMode.Driver => query.Where(x => x.Driver != null && x.Driver.UserId == currentUser.Id),
            _ => query
        };

        List<Order> orders = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        _repositoryManager.Clear();
        return orders;
    }

    public async Task ExecuteActionAsync(RoleOrderCabinetMode mode, uint orderId, RoleOrderAction action, CancellationToken cancellationToken = default)
    {
        AuthenticatedUser currentUser = GetCurrentUser();
        Order order = await _repositoryManager.Order.GetOrderByIdDetailedAsync(orderId, trackChanges: true, cancellationToken)
            ?? throw new InvalidOperationException("Заказ не найден.");

        switch (mode)
        {
            case RoleOrderCabinetMode.Receiver:
                ApplyReceiverAction(order, action, currentUser);
                break;
            case RoleOrderCabinetMode.Driver:
                ApplyDriverAction(order, action, currentUser);
                break;
            default:
                throw new InvalidOperationException("Неподдерживаемый режим кабинета.");
        }

        _repositoryManager.Order.UpdateOrder(order);
        await _repositoryManager.SaveAsync(cancellationToken);
        await LogAsync(order, action, currentUser, cancellationToken);
        _repositoryManager.Clear();
    }

    private void ApplyReceiverAction(Order order, RoleOrderAction action, AuthenticatedUser currentUser)
    {
        if (order.ReceiverUserId != currentUser.Id)
        {
            throw new InvalidOperationException("Этот заказ не принадлежит текущему получателю.");
        }

        if (action != RoleOrderAction.ConfirmReceipt)
        {
            throw new InvalidOperationException("Для получателя доступно только подтверждение получения.");
        }

        if (order.Status != Order.OrderStatuses.Delivered)
        {
            throw new InvalidOperationException("Подтвердить можно только доставленный заказ.");
        }

        string oldStatus = order.Status;
        DateTime now = DateTime.Now;

        order.Status = Order.OrderStatuses.Received;
        order.ReceivedAt = now;
        order.UpdatedAt = now;

        AppendStatusHistory(order, oldStatus, order.Status, currentUser.Id, now, "Получатель подтвердил получение заказа.");
    }

    private void ApplyDriverAction(Order order, RoleOrderAction action, AuthenticatedUser currentUser)
    {
        if (order.Driver?.UserId != currentUser.Id)
        {
            throw new InvalidOperationException("Этот заказ не назначен текущему водителю.");
        }

        string oldStatus = order.Status;
        DateTime now = DateTime.Now;

        switch (action)
        {
            case RoleOrderAction.Accept:
                EnsureStatus(order.Status, Order.OrderStatuses.Assigned, "Принять можно только назначенный заказ.");
                order.Status = Order.OrderStatuses.Accepted;
                SetTransportState(order, "on_route");
                AppendStatusHistory(order, oldStatus, order.Status, currentUser.Id, now, "Водитель принял заказ.");
                break;
            case RoleOrderAction.Refuse:
                EnsureStatus(order.Status, Order.OrderStatuses.Assigned, "Отказаться можно только от назначенного заказа.");
                order.Status = Order.OrderStatuses.Created;
                order.DriverId = null;
                order.VehicleId = null;
                SetTransportState(order, "available");
                AppendStatusHistory(order, oldStatus, order.Status, currentUser.Id, now, "Водитель отказался от заказа. Требуется повторное назначение.");
                break;
            case RoleOrderAction.StartLoading:
                EnsureStatus(order.Status, Order.OrderStatuses.Accepted, "Начать загрузку можно только после принятия заказа.");
                order.Status = Order.OrderStatuses.Loading;
                order.ActualPickupAt ??= now;
                SetTransportState(order, "on_route");
                AppendStatusHistory(order, oldStatus, order.Status, currentUser.Id, now, "Водитель начал загрузку.");
                break;
            case RoleOrderAction.MarkDelivered:
                if (order.Status is not (Order.OrderStatuses.Loading or Order.OrderStatuses.InTransit or Order.OrderStatuses.Accepted))
                {
                    throw new InvalidOperationException("Отметить доставку можно только после принятия или начала загрузки.");
                }

                order.Status = Order.OrderStatuses.Delivered;
                order.ActualPickupAt ??= now;
                order.ActualDeliveryAt = now;
                SetTransportState(order, "available");
                AppendStatusHistory(order, oldStatus, order.Status, currentUser.Id, now, "Водитель отметил заказ как доставленный.");
                break;
            default:
                throw new InvalidOperationException("Это действие недоступно водителю.");
        }

        order.UpdatedAt = now;
    }

    private static void EnsureStatus(string actualStatus, string expectedStatus, string message)
    {
        if (!string.Equals(actualStatus, expectedStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void SetTransportState(Order order, string status)
    {
        if (order.Driver is not null)
        {
            order.Driver.Status = status;
            order.Driver.UpdatedAt = DateTime.Now;
        }

        if (order.Vehicle is not null)
        {
            order.Vehicle.Status = status;
            order.Vehicle.UpdatedAt = DateTime.Now;
        }
    }

    private void AppendStatusHistory(Order order, string oldStatus, string newStatus, uint changedByUserId, DateTime changedAt, string comment)
    {
        var historyItem = new OrderStatusHistory
        {
            OrderId = order.Id,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedByUserId = changedByUserId,
            ChangedAt = changedAt,
            Comment = comment
        };

        order.StatusHistory.Add(historyItem);
        _repositoryManager.OrderStatusHistory.CreateOrderStatusHistory(historyItem);
    }

    private async Task LogAsync(Order order, RoleOrderAction action, AuthenticatedUser currentUser, CancellationToken cancellationToken)
    {
        string actionCode = action switch
        {
            RoleOrderAction.Accept => "driver_order_accepted",
            RoleOrderAction.Refuse => "driver_order_refused",
            RoleOrderAction.StartLoading => "driver_loading_started",
            RoleOrderAction.MarkDelivered => "driver_order_delivered",
            RoleOrderAction.ConfirmReceipt => "receiver_order_received",
            _ => "order_status_changed"
        };

        string description = action switch
        {
            RoleOrderAction.Accept => $"Водитель {currentUser.FullName} принял заказ {order.OrderNumber}",
            RoleOrderAction.Refuse => $"Водитель {currentUser.FullName} отказался от заказа {order.OrderNumber}",
            RoleOrderAction.StartLoading => $"По заказу {order.OrderNumber} начата загрузка",
            RoleOrderAction.MarkDelivered => $"Водитель {currentUser.FullName} отметил заказ {order.OrderNumber} как доставленный",
            RoleOrderAction.ConfirmReceipt => $"Получатель {currentUser.FullName} подтвердил получение заказа {order.OrderNumber}",
            _ => $"Изменен статус заказа {order.OrderNumber}"
        };

        _repositoryManager.ActivityLog.CreateActivityLog(new ActivityLog
        {
            UserId = currentUser.Id,
            EntityType = "order",
            EntityId = order.Id,
            ActionCode = actionCode,
            Description = description,
            CreatedAt = DateTime.Now
        });

        await _repositoryManager.SaveAsync(cancellationToken);
    }

    private AuthenticatedUser GetCurrentUser() =>
        _authStateService.CurrentUser
        ?? throw new InvalidOperationException("Не удалось определить текущего пользователя.");
}
