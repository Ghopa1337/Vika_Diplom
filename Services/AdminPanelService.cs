using CargoTransport.Desktop.Models;
using CargoTransport.Desktop.Repositories;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;
using System.Globalization;
using System.Text.Json;

namespace CargoTransport.Desktop.Services;

public sealed record AdminStatData(string Title, string Value, string Hint);
public sealed record AdminActivityData(string Time, string Title, string Description);
public sealed record AdminUserRowData(
    uint Id,
    string Username,
    string FullName,
    string Role,
    string Status,
    string Phone,
    string LastLogin,
    uint RoleId,
    string? Email,
    string? PhoneRaw,
    string? CompanyName,
    bool IsActive,
    bool IsBlocked,
    bool MustChangePassword);
public sealed record AdminOrderRowData(
    uint Id,
    string OrderNumber,
    string Receiver,
    string Driver,
    string Vehicle,
    string Status,
    string DeliveryDate,
    uint ReceiverUserId,
    uint CargoId,
    uint? DriverId,
    uint? VehicleId,
    string PickupAddress,
    string DeliveryAddress,
    string? PickupContactName,
    string? PickupContactPhone,
    string? DeliveryContactName,
    string? DeliveryContactPhone,
    decimal? DistanceKm,
    decimal? TotalCost,
    string StatusCode,
    DateTime? PlannedPickupAt,
    DateTime? DesiredDeliveryAt,
    string? CancellationReason,
    string? Comment);
public sealed record AdminOrderRequestRowData(
    uint Id,
    uint ReceiverUserId,
    string Receiver,
    string CargoDescription,
    string PickupAddress,
    string DeliveryAddress,
    string PickupContactPhone,
    string DeliveryContactPhone,
    string DesiredDate,
    DateTime? DesiredDateValue,
    string Status,
    string? Comment);
public sealed record AdminCargoRowData(
    uint Id,
    string Name,
    string CargoType,
    string Weight,
    string Volume,
    string UsageCount,
    string UpdatedAt,
    string? Description,
    string? SpecialRequirements);
public sealed record AdminDriverRowData(
    uint Id,
    string FullName,
    string Status,
    string LicenseNumber,
    string Experience,
    string Phone,
    string CurrentOrder);
public sealed record AdminVehicleRowData(
    uint Id,
    string LicensePlate,
    string Model,
    string BodyType,
    string Capacity,
    string Status,
    string AssignedDriver);
public sealed record AdminReportCardData(string Title, string Description, string Freshness, string Format);
public sealed record AdminDashboardData(IReadOnlyList<AdminStatData> Metrics, IReadOnlyList<AdminActivityData> RecentActivities);
public sealed record AdminUsersData(IReadOnlyList<AdminStatData> Stats, IReadOnlyList<AdminUserRowData> Users);
public sealed record AdminOrdersData(IReadOnlyList<AdminStatData> Stats, IReadOnlyList<AdminOrderRowData> Orders, IReadOnlyList<AdminOrderRequestRowData> Requests);
public sealed record AdminCargoData(IReadOnlyList<AdminStatData> Stats, IReadOnlyList<AdminCargoRowData> Cargo);
public sealed record AdminDriversData(IReadOnlyList<AdminStatData> Stats, IReadOnlyList<AdminDriverRowData> Drivers);
public sealed record AdminVehiclesData(IReadOnlyList<AdminStatData> Stats, IReadOnlyList<AdminVehicleRowData> Vehicles);
public sealed record AdminReportsData(IReadOnlyList<AdminStatData> Stats, IReadOnlyList<AdminReportCardData> Reports);
public sealed record AdminPanelData(
    AdminDashboardData Dashboard,
    AdminUsersData Users,
    AdminOrdersData Orders,
    AdminCargoData Cargo,
    AdminDriversData Drivers,
    AdminVehiclesData Vehicles,
    AdminReportsData Reports);

public sealed record AdminPanelQuery(
    SieveModel? Users = null,
    SieveModel? Orders = null,
    SieveModel? Cargo = null,
    SieveModel? Drivers = null,
    SieveModel? Vehicles = null);

public interface IAdminPanelService
{
    Task<AdminPanelData> GetAdminPanelDataAsync(AdminPanelQuery? query = null, CancellationToken cancellationToken = default);
}

public sealed class AdminPanelService : IAdminPanelService
{
    private static readonly CultureInfo RuCulture = CultureInfo.GetCultureInfo("ru-RU");
    private readonly IRepositoryManager _repositoryManager;
    private readonly ISieveProcessor _sieveProcessor;

    public AdminPanelService(
        IRepositoryManager repositoryManager,
        ISieveProcessor sieveProcessor)
    {
        _repositoryManager = repositoryManager;
        _sieveProcessor = sieveProcessor;
    }

    public async Task<AdminPanelData> GetAdminPanelDataAsync(AdminPanelQuery? query = null, CancellationToken cancellationToken = default)
    {
        List<User> dashboardUsers = await _repositoryManager.User
            .GetAllUsersWithRoles(trackChanges: false)
            .OrderBy(x => x.Role.Name)
            .ThenBy(x => x.Username)
            .ToListAsync(cancellationToken);

        List<Driver> dashboardDrivers = await _repositoryManager.Driver
            .GetAllDriversWithUsers(trackChanges: false)
            .OrderBy(x => x.User.FullName)
            .ToListAsync(cancellationToken);

        List<Vehicle> dashboardVehicles = await _repositoryManager.Vehicle
            .GetAllVehiclesWithDrivers(trackChanges: false)
            .OrderBy(x => x.LicensePlate)
            .ToListAsync(cancellationToken);

        List<Order> dashboardOrders = await _repositoryManager.Order
            .GetAllOrdersDetailed(trackChanges: false)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        List<OrderRequest> dashboardRequests = await _repositoryManager.OrderRequest
            .GetPendingRequestsDetailed(trackChanges: false)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        List<CargoItem> dashboardCargo = await _repositoryManager.Cargo
            .GetAllCargo(trackChanges: false)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        IQueryable<User> usersQuery = ApplySieve(
            query?.Users,
            _repositoryManager.User.GetAllUsersWithRoles(trackChanges: false));
        if (string.IsNullOrWhiteSpace(query?.Users?.Sorts))
        {
            usersQuery = usersQuery.OrderBy(x => x.Role.Name).ThenBy(x => x.Username);
        }

        List<User> users = await usersQuery.ToListAsync(cancellationToken);

        IQueryable<Driver> driversQuery = ApplySieve(
            query?.Drivers,
            _repositoryManager.Driver.GetAllDriversWithUsers(trackChanges: false));
        if (string.IsNullOrWhiteSpace(query?.Drivers?.Sorts))
        {
            driversQuery = driversQuery.OrderBy(x => x.User.FullName);
        }

        List<Driver> drivers = await driversQuery.ToListAsync(cancellationToken);

        IQueryable<Vehicle> vehiclesQuery = ApplySieve(
            query?.Vehicles,
            _repositoryManager.Vehicle.GetAllVehiclesWithDrivers(trackChanges: false));
        if (string.IsNullOrWhiteSpace(query?.Vehicles?.Sorts))
        {
            vehiclesQuery = vehiclesQuery.OrderBy(x => x.LicensePlate);
        }

        List<Vehicle> vehicles = await vehiclesQuery.ToListAsync(cancellationToken);

        IQueryable<Order> ordersQuery = ApplySieve(
            query?.Orders,
            _repositoryManager.Order.GetAllOrdersDetailed(trackChanges: false));
        if (string.IsNullOrWhiteSpace(query?.Orders?.Sorts))
        {
            ordersQuery = ordersQuery.OrderByDescending(x => x.CreatedAt);
        }

        List<Order> orders = await ordersQuery.ToListAsync(cancellationToken);

        IQueryable<CargoItem> cargoQuery = ApplySieve(
            query?.Cargo,
            _repositoryManager.Cargo.GetAllCargo(trackChanges: false));
        if (string.IsNullOrWhiteSpace(query?.Cargo?.Sorts))
        {
            cargoQuery = cargoQuery.OrderByDescending(x => x.UpdatedAt);
        }

        List<CargoItem> cargo = await cargoQuery.ToListAsync(cancellationToken);

        List<ActivityLog> activities = await _repositoryManager.ActivityLog
            .GetRecentActivityLogsWithUsers(takeCount: 4, trackChanges: false)
            .ToListAsync(cancellationToken);

        List<Report> reports = await _repositoryManager.Report
            .GetRecentReports(trackChanges: false)
            .Take(10)
            .ToListAsync(cancellationToken);

        _repositoryManager.Clear();

        return new AdminPanelData(
            BuildDashboardData(dashboardUsers, dashboardDrivers, dashboardVehicles, dashboardOrders, dashboardRequests.Count, activities),
            BuildUsersData(users),
            BuildOrdersData(orders, dashboardRequests),
            BuildCargoData(cargo, dashboardOrders),
            BuildDriversData(drivers, dashboardOrders),
            BuildVehiclesData(vehicles, dashboardOrders),
            BuildReportsData(reports));
    }

    private IQueryable<T> ApplySieve<T>(SieveModel? model, IQueryable<T> source) where T : class
    {
        if (model is null || (string.IsNullOrWhiteSpace(model.Filters) && string.IsNullOrWhiteSpace(model.Sorts)))
        {
            return source;
        }

        return _sieveProcessor.Apply(model, source, applyPagination: false);
    }

    private static AdminDashboardData BuildDashboardData(
        IReadOnlyCollection<User> users,
        IReadOnlyCollection<Driver> drivers,
        IReadOnlyCollection<Vehicle> vehicles,
        IReadOnlyCollection<Order> orders,
        int pendingRequestsCount,
        IReadOnlyCollection<ActivityLog> activities)
    {
        int activeOrdersCount = orders.Count(IsOrderActive);
        int unassignedOrdersCount = orders.Count(x => x.DriverId is null || x.VehicleId is null);
        int activeUsersCount = users.Count(x => x.IsActive && !x.IsBlocked);
        int blockedUsersCount = users.Count(x => x.IsBlocked);
        int availableVehiclesCount = vehicles.Count(x => x.Status == "available");
        int vehiclesOnRouteCount = vehicles.Count(x => x.Status == "on_route");
        int driversOnRouteCount = drivers.Count(x => x.Status == "on_route");
        int driversRestCount = drivers.Count(x => x.Status is "rest" or "sick");

        var metrics = new List<AdminStatData>
        {
            new("Активные заказы", activeOrdersCount.ToString(RuCulture), $"{unassignedOrdersCount} требуют назначения транспорта"),
            new("Пользователи", users.Count.ToString(RuCulture), $"{activeUsersCount} активны, {blockedUsersCount} заблокированы"),
            new("Транспорт", vehicles.Count.ToString(RuCulture), $"{availableVehiclesCount} доступны, {vehiclesOnRouteCount} в рейсе"),
            new("Водители", drivers.Count.ToString(RuCulture), $"{driversOnRouteCount} на маршруте, {driversRestCount} в отдыхе")
        };

        List<AdminActivityData> recentActivities = activities
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AdminActivityData(
                x.CreatedAt.ToString("HH:mm", RuCulture),
                GetActivityTitle(x),
                x.Description ?? "Описание действия отсутствует."))
            .ToList();

        return new AdminDashboardData(metrics, recentActivities);
    }

    private static AdminUsersData BuildUsersData(IReadOnlyCollection<User> users)
    {
        int activeUsersCount = users.Count(x => x.IsActive && !x.IsBlocked);
        int blockedUsersCount = users.Count(x => x.IsBlocked);
        int inactiveUsersCount = users.Count(x => !x.IsActive);
        DateTime? latestLoginAt = users.Max(x => x.LastLoginAt);
        User? latestLoginUser = latestLoginAt is null
            ? null
            : users
                .Where(x => x.LastLoginAt == latestLoginAt)
                .OrderBy(x => x.FullName)
                .FirstOrDefault();

        string roleBreakdown = string.Join(", ",
            users.GroupBy(x => x.Role.Code)
                .OrderBy(x => x.Key)
                .Select(x => $"{x.Count()} {GetRoleNameForSummary(x.Key, x.Count())}"));

        var stats = new List<AdminStatData>
        {
            new("Всего пользователей", users.Count.ToString(RuCulture), roleBreakdown),
            new("Активные учетные записи", activeUsersCount.ToString(RuCulture), $"{blockedUsersCount} заблокированы, {inactiveUsersCount} неактивны"),
            new(
                "Последний вход",
                latestLoginAt is null ? "Нет данных" : GetRelativeDateLabel(latestLoginAt.Value),
                latestLoginUser is null ? "Пользователи еще не входили в систему" : $"{latestLoginUser.FullName} • {FormatDateTime(latestLoginAt)}")
        };

        List<AdminUserRowData> rows = users
            .OrderBy(x => x.Role.Name)
            .ThenBy(x => x.Username)
            .Select(x => new AdminUserRowData(
                x.Id,
                x.Username,
                x.CompanyName ?? x.FullName,
                x.Role.Name,
                GetUserStatus(x),
                x.Phone ?? "Не указан",
                FormatDateTime(x.LastLoginAt),
                x.RoleId,
                x.Email,
                x.Phone,
                x.CompanyName,
                x.IsActive,
                x.IsBlocked,
                x.MustChangePassword))
            .ToList();

        return new AdminUsersData(stats, rows);
    }

    private static AdminOrdersData BuildOrdersData(IReadOnlyCollection<Order> orders)
    {
        int newOrdersCount = orders.Count(x => x.Status == "created");
        int inTransitOrdersCount = orders.Count(x => x.Status is "loading" or "in_transit");
        List<Order> overdueOrders = orders
            .Where(x => x.DesiredDeliveryAt.HasValue
                && x.DesiredDeliveryAt.Value < DateTime.Now
                && x.Status is not ("delivered" or "received" or "cancelled"))
            .OrderBy(x => x.DesiredDeliveryAt)
            .ToList();

        var stats = new List<AdminStatData>
        {
            new("Новые заказы", newOrdersCount.ToString(RuCulture), newOrdersCount > 0 ? "Ожидают назначения диспетчером" : "Нет новых заявок"),
            new("В пути", inTransitOrdersCount.ToString(RuCulture), $"{orders.Count(x => x.Status == "assigned")} назначены, {orders.Count(x => x.Status == "loading")} на погрузке"),
            new(
                "Просроченные",
                overdueOrders.Count.ToString(RuCulture),
                overdueOrders.Count == 0 ? "Просроченных доставок нет" : string.Join(", ", overdueOrders.Take(3).Select(x => x.OrderNumber)))
        };

        List<AdminOrderRowData> rows = orders
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AdminOrderRowData(
                x.Id,
                x.OrderNumber,
                x.ReceiverUser.CompanyName ?? x.ReceiverUser.FullName,
                x.Driver?.User is null ? "Не назначен" : FormatShortName(x.Driver.User.FullName),
                x.Vehicle?.LicensePlate ?? "Не назначено",
                GetOrderStatusName(x.Status),
                FormatDateTime(x.DesiredDeliveryAt),
                x.ReceiverUserId,
                x.CargoId,
                x.DriverId,
                x.VehicleId,
                x.PickupAddress,
                x.DeliveryAddress,
                x.PickupContactName,
                x.PickupContactPhone,
                x.DeliveryContactName,
                x.DeliveryContactPhone,
                x.DistanceKm,
                x.TotalCost,
                x.Status,
                x.PlannedPickupAt,
                x.DesiredDeliveryAt,
                x.CancellationReason,
                x.Comment))
            .ToList();

        return new AdminOrdersData(stats, rows, []);
    }

    private static AdminOrdersData BuildOrdersData(IReadOnlyCollection<Order> orders, IReadOnlyCollection<OrderRequest> requests)
    {
        AdminOrdersData baseData = BuildOrdersData(orders);
        List<AdminOrderRequestRowData> requestRows = requests
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AdminOrderRequestRowData(
                x.Id,
                x.ReceiverUserId,
                x.ReceiverUser.CompanyName ?? x.ReceiverUser.FullName,
                x.CargoDescription,
                x.PickupAddress,
                x.DeliveryAddress,
                x.PickupContactPhone,
                x.DeliveryContactPhone,
                FormatDateTime(x.DesiredDate),
                x.DesiredDate,
                GetOrderRequestStatusName(x.Status),
                x.Comment))
            .ToList();

        IReadOnlyList<AdminStatData> stats =
        [
            new AdminStatData("Новые заявки", requests.Count.ToString(RuCulture), requests.Count > 0 ? "Ожидают оформления в заказ" : "Новых заявок сейчас нет"),
            .. baseData.Stats.Skip(1)
        ];

        return new AdminOrdersData(stats, baseData.Orders, requestRows);
    }

    private static AdminCargoData BuildCargoData(IReadOnlyCollection<CargoItem> cargo, IReadOnlyCollection<Order> orders)
    {
        int hazardousCount = cargo.Count(x => x.CargoType == "hazardous");
        int oversizedCount = cargo.Count(x => x.CargoType == "oversized");
        int usedInOrdersCount = cargo.Count(x => orders.Any(order => order.CargoId == x.Id));

        var stats = new List<AdminStatData>
        {
            new("Всего грузов", cargo.Count.ToString(RuCulture), $"{usedInOrdersCount} уже используются в заказах"),
            new("Опасные", hazardousCount.ToString(RuCulture), $"{oversizedCount} крупногабаритных позиций в справочнике"),
            new("Тяжёлые", cargo.Count(x => x.WeightKg >= 1000).ToString(RuCulture), "Грузы с массой от 1000 кг и выше")
        };

        List<AdminCargoRowData> rows = cargo
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new AdminCargoRowData(
                x.Id,
                x.Name,
                GetCargoTypeName(x.CargoType),
                $"{x.WeightKg:0.##} кг",
                x.VolumeM3.HasValue ? $"{x.VolumeM3.Value:0.##} м3" : "Не указан",
                orders.Count(order => order.CargoId == x.Id).ToString(RuCulture),
                FormatDateTime(x.UpdatedAt),
                x.Description,
                x.SpecialRequirements))
            .ToList();

        return new AdminCargoData(stats, rows);
    }

    private static AdminDriversData BuildDriversData(IReadOnlyCollection<Driver> drivers, IReadOnlyCollection<Order> orders)
    {
        int availableDriversCount = drivers.Count(x => x.Status == "available" && GetCurrentOrderForDriver(x.Id, orders) is null);
        int driversOnRouteCount = drivers.Count(x => x.Status == "on_route");
        int unavailableDriversCount = drivers.Count(x => x.Status is "rest" or "sick");

        var stats = new List<AdminStatData>
        {
            new("Доступны", availableDriversCount.ToString(RuCulture), "Можно назначать на новые рейсы"),
            new("На маршруте", driversOnRouteCount.ToString(RuCulture), "Требуется оперативный контроль"),
            new("Отдых / недоступны", unavailableDriversCount.ToString(RuCulture), "Планирование следующей смены")
        };

        List<AdminDriverRowData> rows = drivers
            .OrderBy(x => x.User.FullName)
            .Select(x =>
            {
                Order? currentOrder = GetCurrentOrderForDriver(x.Id, orders);
                return new AdminDriverRowData(
                    x.Id,
                    x.User.FullName,
                    GetDriverDisplayStatus(x, currentOrder),
                    x.LicenseNumber,
                    FormatYears(x.ExperienceYears, "год", "года", "лет"),
                    x.User.Phone ?? "Не указан",
                    currentOrder?.OrderNumber ?? "Нет");
            })
            .ToList();

        return new AdminDriversData(stats, rows);
    }

    private static AdminVehiclesData BuildVehiclesData(IReadOnlyCollection<Vehicle> vehicles, IReadOnlyCollection<Order> orders)
    {
        int availableVehiclesCount = vehicles.Count(x => x.Status == "available" && GetCurrentOrderForVehicle(x.Id, orders) is null);
        int vehiclesOnRouteCount = vehicles.Count(x => x.Status == "on_route");
        int vehiclesOnRepairCount = vehicles.Count(x => x.Status == "repair");

        var stats = new List<AdminStatData>
        {
            new("Доступны", availableVehiclesCount.ToString(RuCulture), "Можно закреплять за новыми заказами"),
            new("В рейсе", vehiclesOnRouteCount.ToString(RuCulture), "Используются в активных перевозках"),
            new("Ремонт", vehiclesOnRepairCount.ToString(RuCulture), "Временно исключены из назначения")
        };

        List<AdminVehicleRowData> rows = vehicles
            .OrderBy(x => x.LicensePlate)
            .Select(x =>
            {
                Order? currentOrder = GetCurrentOrderForVehicle(x.Id, orders);
                return new AdminVehicleRowData(
                    x.Id,
                    x.LicensePlate,
                    x.Model,
                    GetBodyTypeName(x.BodyType),
                    $"{x.CapacityKg:0} кг",
                    GetVehicleDisplayStatus(x, currentOrder),
                    x.CurrentDriver?.User is null ? "Не закреплен" : FormatShortName(x.CurrentDriver.User.FullName));
            })
            .ToList();

        return new AdminVehiclesData(stats, rows);
    }

    private static AdminReportsData BuildReportsData(IReadOnlyCollection<Report> reports)
    {
        List<ReportPayload> payloads = reports
            .Select(ParseReportPayload)
            .ToList();

        int todayReportsCount = reports.Count(x => x.CreatedAt.Date == DateTime.Today);
        Report? latestReport = reports.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        string availableFormats = string.Join(" / ",
            payloads.Select(x => x.Format)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase));

        var stats = new List<AdminStatData>
        {
            new("Сводки сегодня", todayReportsCount.ToString(RuCulture), "Актуальные отчеты за текущую дату"),
            new(
                "Последний отчет",
                latestReport is null ? "Нет данных" : FormatDateTime(latestReport.CreatedAt),
                latestReport is null ? "Отчеты еще не сформированы" : ParseReportPayload(latestReport).Title),
            new("Форматы", string.IsNullOrWhiteSpace(availableFormats) ? "Нет данных" : availableFormats, $"{reports.Count} записей в журнале отчетов")
        };

        List<AdminReportCardData> cards = reports
            .OrderByDescending(x => x.CreatedAt)
            .Select(x =>
            {
                ReportPayload payload = ParseReportPayload(x);
                return new AdminReportCardData(
                    payload.Title,
                    payload.Description,
                    payload.Freshness,
                    payload.Format);
            })
            .ToList();

        return new AdminReportsData(stats, cards);
    }

    private static ReportPayload ParseReportPayload(Report report)
    {
        try
        {
            ReportPayload? payload = JsonSerializer.Deserialize<ReportPayload>(report.ReportData, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payload is not null)
            {
                return payload with
                {
                    Title = string.IsNullOrWhiteSpace(payload.Title) ? GetReportTypeName(report.ReportType) : payload.Title,
                    Description = string.IsNullOrWhiteSpace(payload.Description) ? "Описание отчета отсутствует." : payload.Description,
                    Format = string.IsNullOrWhiteSpace(payload.Format) ? "PDF" : payload.Format,
                    Freshness = string.IsNullOrWhiteSpace(payload.Freshness) ? $"Сформирован {FormatDateTime(report.CreatedAt)}" : payload.Freshness
                };
            }
        }
        catch (JsonException)
        {
        }

        return new ReportPayload(
            GetReportTypeName(report.ReportType),
            "Описание отчета отсутствует.",
            $"Сформирован {FormatDateTime(report.CreatedAt)}",
            "PDF");
    }

    private static string GetActivityTitle(ActivityLog activityLog) =>
        activityLog.ActionCode switch
        {
            "login" => "Вход в систему",
            "order_status_changed" => "Статус заказа изменен",
            "order_delivery_confirmed" => "Подтверждена доставка",
            "driver_created" => "Новый водитель",
            "vehicle_repair_started" => "Транспорт отправлен в ремонт",
            _ => activityLog.EntityType switch
            {
                "order" => "Событие по заказу",
                "driver" => "Событие по водителю",
                "vehicle" => "Событие по транспорту",
                _ => "Действие пользователя"
            }
        };

    private static string GetUserStatus(User user) =>
        !user.IsActive
            ? "Неактивен"
            : user.IsBlocked
                ? "Заблокирован"
                : "Активен";

    private static string GetOrderStatusName(string status) =>
        status switch
        {
            "created" => "Создан",
            "assigned" => "Назначен",
            "accepted" => "Принят",
            "loading" => "Погрузка",
            "in_transit" => "В пути",
            "delivered" => "Доставлен",
            "received" => "Получен",
            "cancelled" => "Отменен",
            _ => status
        };

    private static string GetOrderRequestStatusName(string status) =>
        status switch
        {
            "pending" => "Ожидает обработки",
            "processed" => "Преобразована в заказ",
            _ => status
        };

    private static string GetDriverDisplayStatus(Driver driver, Order? currentOrder)
    {
        if (driver.Status == "available" && currentOrder?.Status == "assigned")
        {
            return "Назначен";
        }

        return driver.Status switch
        {
            "available" => "Доступен",
            "on_route" => "В рейсе",
            "rest" => "Отдых",
            "sick" => "Болен",
            _ => driver.Status
        };
    }

    private static string GetVehicleDisplayStatus(Vehicle vehicle, Order? currentOrder)
    {
        if (vehicle.Status == "available" && currentOrder?.Status == "assigned")
        {
            return "Назначен";
        }

        return vehicle.Status switch
        {
            "available" => "Доступен",
            "on_route" => "В рейсе",
            "repair" => "Ремонт",
            "decommissioned" => "Списан",
            _ => vehicle.Status
        };
    }

    private static string GetCargoTypeName(string? cargoType) =>
        cargoType switch
        {
            "normal" => "Обычный",
            "hazardous" => "Опасный",
            "perishable" => "Скоропортящийся",
            "oversized" => "Крупногабаритный",
            null or "" => "Не указан",
            _ => cargoType
        };

    private static string GetBodyTypeName(string? bodyType) =>
        bodyType switch
        {
            "curtain" => "Тент",
            "refrigerator" => "Рефрижератор",
            "van" => "Фургон",
            "flatbed" => "Платформа",
            null or "" => "Не указан",
            _ => bodyType
        };

    private static string GetReportTypeName(string reportType) =>
        reportType switch
        {
            "orders" => "Отчет по заказам",
            "drivers" => "Нагрузка водителей",
            "vehicles" => "Состояние автопарка",
            "finance" => "Финансовая сводка",
            _ => reportType
        };

    private static string GetRelativeDateLabel(DateTime dateTime) =>
        dateTime.Date == DateTime.Today
            ? "Сегодня"
            : dateTime.Date == DateTime.Today.AddDays(-1)
                ? "Вчера"
                : dateTime.ToString("dd.MM.yyyy", RuCulture);

    private static string FormatDateTime(DateTime? value) =>
        value.HasValue
            ? value.Value.ToString("dd.MM.yyyy HH:mm", RuCulture)
            : "Нет данных";

    private static string FormatShortName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "Не указано";
        }

        string[] parts = fullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length < 2)
        {
            return fullName;
        }

        string initials = string.Concat(parts.Skip(1).Take(2).Select(x => $"{x[0]}."));
        return $"{parts[0]} {initials}";
    }

    private static string FormatYears(int years, string one, string twoToFour, string other)
    {
        int value = Math.Abs(years) % 100;
        int digit = value % 10;
        string suffix = value is > 10 and < 20
            ? other
            : digit switch
            {
                1 => one,
                >= 2 and <= 4 => twoToFour,
                _ => other
            };

        return $"{years} {suffix}";
    }

    private static string GetRoleNameForSummary(string roleCode, int count) =>
        roleCode switch
        {
            "admin" => GetPluralForm(count, "администратор", "администратора", "администраторов"),
            "dispatcher" => GetPluralForm(count, "диспетчер", "диспетчера", "диспетчеров"),
            "receiver" => GetPluralForm(count, "получатель", "получателя", "получателей"),
            "driver" => GetPluralForm(count, "водитель", "водителя", "водителей"),
            _ => "пользователей"
        };

    private static string GetPluralForm(int count, string one, string twoToFour, string other)
    {
        int value = Math.Abs(count) % 100;
        int digit = value % 10;
        return value is > 10 and < 20
            ? other
            : digit switch
            {
                1 => one,
                >= 2 and <= 4 => twoToFour,
                _ => other
            };
    }

    private static bool IsOrderActive(Order order) =>
        order.Status is not ("delivered" or "received" or "cancelled");

    private static Order? GetCurrentOrderForDriver(uint driverId, IEnumerable<Order> orders) =>
        orders
            .Where(x => x.DriverId == driverId && x.Status is not ("delivered" or "received" or "cancelled"))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();

    private static Order? GetCurrentOrderForVehicle(uint vehicleId, IEnumerable<Order> orders) =>
        orders
            .Where(x => x.VehicleId == vehicleId && x.Status is not ("delivered" or "received" or "cancelled"))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();

    private sealed record ReportPayload(string Title, string Description, string Freshness, string Format);
}
