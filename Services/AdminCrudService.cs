using CargoTransport.Desktop.Models;
using CargoTransport.Desktop.Repositories;
using Microsoft.EntityFrameworkCore;
using CargoTransport.Desktop;

namespace CargoTransport.Desktop.Services;

public sealed record AdminLookupItemData(uint Id, string DisplayName);

public sealed record AdminUserEditData(
    uint? Id,
    string Username,
    string? Password,
    string FullName,
    uint RoleId,
    string? Email,
    string? Phone,
    string? CompanyName,
    bool IsActive,
    bool IsBlocked,
    bool MustChangePassword);

public sealed record AdminDriverEditData(
    uint? Id,
    uint UserId,
    string LicenseNumber,
    string LicenseCategory,
    ushort ExperienceYears,
    string Status,
    string? Notes);

public sealed record AdminVehicleEditData(
    uint? Id,
    string Model,
    string LicensePlate,
    decimal CapacityKg,
    decimal? VolumeM3,
    string? BodyType,
    ushort? ProductionYear,
    string Status,
    DateTime? InsuranceExpiry,
    uint? CurrentDriverId,
    string? Notes);

public sealed record AdminCargoEditData(
    uint? Id,
    string Name,
    string CargoType,
    decimal WeightKg,
    decimal? VolumeM3,
    string? Description,
    string? SpecialRequirements);

public sealed record AdminOrderEditData(
    uint? Id,
    string OrderNumber,
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
    string Status,
    DateTime? PlannedPickupAt,
    DateTime? DesiredDeliveryAt,
    string? CancellationReason,
    string? Comment);

public interface IAdminCrudService
{
    Task<IReadOnlyList<AdminLookupItemData>> GetRoleOptionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminLookupItemData>> GetDriverUserOptionsAsync(uint? currentDriverId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminLookupItemData>> GetReceiverOptionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminLookupItemData>> GetCargoOptionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminLookupItemData>> GetDriverOptionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminLookupItemData>> GetVehicleDriverOptionsAsync(uint? currentVehicleId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminLookupItemData>> GetVehicleOptionsAsync(CancellationToken cancellationToken = default);
    Task<Order?> GetOrderDetailsAsync(uint orderId, CancellationToken cancellationToken = default);
    Task<AdminCargoEditData?> GetCargoEditDataAsync(uint cargoId, CancellationToken cancellationToken = default);
    Task<AdminDriverEditData?> GetDriverEditDataAsync(uint driverId, CancellationToken cancellationToken = default);
    Task<AdminVehicleEditData?> GetVehicleEditDataAsync(uint vehicleId, CancellationToken cancellationToken = default);
    Task CreateUserAsync(AdminUserEditData data, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(AdminUserEditData data, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(uint userId, CancellationToken cancellationToken = default);
    Task CreateCargoAsync(AdminCargoEditData data, CancellationToken cancellationToken = default);
    Task UpdateCargoAsync(AdminCargoEditData data, CancellationToken cancellationToken = default);
    Task DeleteCargoAsync(uint cargoId, CancellationToken cancellationToken = default);
    Task CreateDriverAsync(AdminDriverEditData data, CancellationToken cancellationToken = default);
    Task UpdateDriverAsync(AdminDriverEditData data, CancellationToken cancellationToken = default);
    Task DeleteDriverAsync(uint driverId, CancellationToken cancellationToken = default);
    Task CreateVehicleAsync(AdminVehicleEditData data, CancellationToken cancellationToken = default);
    Task UpdateVehicleAsync(AdminVehicleEditData data, CancellationToken cancellationToken = default);
    Task DeleteVehicleAsync(uint vehicleId, CancellationToken cancellationToken = default);
    Task<Order> CreateOrderAsync(AdminOrderEditData data, CancellationToken cancellationToken = default);
    Task<Order> CreateOrderWithCargoAsync(AdminOrderEditData data, CargoItem cargo, CancellationToken cancellationToken = default);
    Task UpdateOrderAsync(AdminOrderEditData data, CancellationToken cancellationToken = default);
    Task DeleteOrderAsync(uint orderId, CancellationToken cancellationToken = default);
}

public sealed class AdminCrudService : IAdminCrudService
{
    private readonly IRepositoryManager _repositoryManager;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthStateService _authStateService;

    public AdminCrudService(
        IRepositoryManager repositoryManager,
        IPasswordHasher passwordHasher,
        IAuthStateService authStateService)
    {
        _repositoryManager = repositoryManager;
        _passwordHasher = passwordHasher;
        _authStateService = authStateService;
    }

    public async Task<IReadOnlyList<AdminLookupItemData>> GetRoleOptionsAsync(CancellationToken cancellationToken = default)
    {
        List<AdminLookupItemData> options = await _repositoryManager.Role
            .GetAllRoles(trackChanges: false)
            .OrderBy(x => x.Name)
            .Select(x => new AdminLookupItemData(x.Id, x.Name))
            .ToListAsync(cancellationToken);

        _repositoryManager.Clear();
        return options;
    }

    public async Task<IReadOnlyList<AdminLookupItemData>> GetDriverUserOptionsAsync(uint? currentDriverId = null, CancellationToken cancellationToken = default)
    {
        List<User> users = await _repositoryManager.User
            .GetUsersByRoleCode("driver", trackChanges: false)
            .Where(x => x.DriverProfile == null || (currentDriverId.HasValue && x.DriverProfile.Id == currentDriverId.Value))
            .OrderBy(x => x.FullName)
            .ToListAsync(cancellationToken);

        List<AdminLookupItemData> options = users
            .Select(x => new AdminLookupItemData(x.Id, $"{x.FullName} ({x.Username})"))
            .ToList();

        _repositoryManager.Clear();
        return options;
    }

    public async Task<IReadOnlyList<AdminLookupItemData>> GetReceiverOptionsAsync(CancellationToken cancellationToken = default)
    {
        List<User> users = await _repositoryManager.User
            .GetUsersByRoleCode("receiver", trackChanges: false)
            .Where(x => x.IsActive && !x.IsBlocked)
            .OrderBy(x => x.CompanyName ?? x.FullName)
            .ToListAsync(cancellationToken);

        List<AdminLookupItemData> options = users
            .Select(x => new AdminLookupItemData(x.Id, x.CompanyName ?? x.FullName))
            .ToList();

        _repositoryManager.Clear();
        return options;
    }

    public async Task<IReadOnlyList<AdminLookupItemData>> GetCargoOptionsAsync(CancellationToken cancellationToken = default)
    {
        List<CargoItem> cargoItems = await _repositoryManager.Cargo
            .GetAllCargo(trackChanges: false)
            .ToListAsync(cancellationToken);

        List<AdminLookupItemData> options = cargoItems
            .Select(x => new AdminLookupItemData(x.Id, $"{x.Name} ({x.WeightKg:0} кг)"))
            .ToList();

        _repositoryManager.Clear();
        return options;
    }

    public async Task<IReadOnlyList<AdminLookupItemData>> GetDriverOptionsAsync(CancellationToken cancellationToken = default)
    {
        List<Driver> drivers = await _repositoryManager.Driver
            .GetAllDriversWithUsers(trackChanges: false)
            .OrderBy(x => x.User.FullName)
            .ToListAsync(cancellationToken);

        List<AdminLookupItemData> options = drivers
            .Select(x => new AdminLookupItemData(x.Id, $"{x.User.FullName} ({x.LicenseNumber})"))
            .ToList();

        _repositoryManager.Clear();
        return options;
    }

    public async Task<IReadOnlyList<AdminLookupItemData>> GetVehicleDriverOptionsAsync(uint? currentVehicleId = null, CancellationToken cancellationToken = default)
    {
        List<Driver> drivers = await _repositoryManager.Driver
            .GetAllDriversWithUsers(trackChanges: false)
            .Include(x => x.CurrentVehicle)
            .Where(x => x.CurrentVehicle == null || (currentVehicleId.HasValue && x.CurrentVehicle.Id == currentVehicleId.Value))
            .OrderBy(x => x.User.FullName)
            .ToListAsync(cancellationToken);

        List<AdminLookupItemData> options = drivers
            .Select(x => new AdminLookupItemData(x.Id, $"{x.User.FullName} ({x.LicenseNumber})"))
            .ToList();

        _repositoryManager.Clear();
        return options;
    }

    public async Task<IReadOnlyList<AdminLookupItemData>> GetVehicleOptionsAsync(CancellationToken cancellationToken = default)
    {
        List<Vehicle> vehicles = await _repositoryManager.Vehicle
            .GetAllVehiclesWithDrivers(trackChanges: false)
            .OrderBy(x => x.LicensePlate)
            .ToListAsync(cancellationToken);

        List<AdminLookupItemData> options = vehicles
            .Select(x => new AdminLookupItemData(x.Id, $"{x.LicensePlate} ({x.Model})"))
            .ToList();

        _repositoryManager.Clear();
        return options;
    }

    public async Task<Order?> GetOrderDetailsAsync(uint orderId, CancellationToken cancellationToken = default)
    {
        Order? order = await _repositoryManager.Order
            .GetOrderByIdDetailedAsync(orderId, trackChanges: false, cancellationToken);

        _repositoryManager.Clear();
        return order;
    }

    public async Task<AdminDriverEditData?> GetDriverEditDataAsync(uint driverId, CancellationToken cancellationToken = default)
    {
        Driver? driver = await _repositoryManager.Driver
            .GetDriverByIdWithUserAsync(driverId, trackChanges: false, cancellationToken);

        _repositoryManager.Clear();

        return driver is null
            ? null
            : new AdminDriverEditData(
                driver.Id,
                driver.UserId,
                driver.LicenseNumber,
                driver.LicenseCategory,
                driver.ExperienceYears,
                driver.Status,
                driver.Notes);
    }

    public async Task<AdminVehicleEditData?> GetVehicleEditDataAsync(uint vehicleId, CancellationToken cancellationToken = default)
    {
        Vehicle? vehicle = await _repositoryManager.Vehicle
            .GetVehicleByIdWithDriverAsync(vehicleId, trackChanges: false, cancellationToken);

        _repositoryManager.Clear();

        return vehicle is null
            ? null
            : new AdminVehicleEditData(
                vehicle.Id,
                vehicle.Model,
                vehicle.LicensePlate,
                vehicle.CapacityKg,
                vehicle.VolumeM3,
                vehicle.BodyType,
                vehicle.ProductionYear,
                vehicle.Status,
                vehicle.InsuranceExpiry?.ToDateTime(TimeOnly.MinValue),
                vehicle.CurrentDriverId,
                vehicle.Notes);
    }

    public async Task<AdminCargoEditData?> GetCargoEditDataAsync(uint cargoId, CancellationToken cancellationToken = default)
    {
        CargoItem? cargo = await _repositoryManager.Cargo
            .GetCargoByIdAsync(cargoId, trackChanges: false, cancellationToken);

        _repositoryManager.Clear();

        return cargo is null
            ? null
            : new AdminCargoEditData(
                cargo.Id,
                cargo.Name,
                cargo.CargoType,
                cargo.WeightKg,
                cargo.VolumeM3,
                cargo.Description,
                cargo.SpecialRequirements);
    }

    public async Task CreateUserAsync(AdminUserEditData data, CancellationToken cancellationToken = default)
    {
        ValidateUser(data, isCreate: true);

        string username = data.Username.Trim();
        string? email = InputValidationHelper.NormalizeOptionalEmail(data.Email);

        if (await _repositoryManager.User.ExistsByUsernameAsync(username, cancellationToken))
        {
            throw new InvalidOperationException("Пользователь с таким логином уже существует.");
        }

        if (!string.IsNullOrWhiteSpace(email)
            && await _repositoryManager.User.ExistsByEmailExceptIdAsync(email, 0, cancellationToken))
        {
            throw new InvalidOperationException("Пользователь с такой почтой уже существует.");
        }

        var user = new User
        {
            Username = username,
            PasswordHash = _passwordHasher.HashPassword(data.Password!),
            FullName = data.FullName.Trim(),
            RoleId = data.RoleId,
            Email = email,
            Phone = InputValidationHelper.NormalizeOptionalPhone(data.Phone),
            CompanyName = NormalizeOptional(data.CompanyName),
            IsActive = data.IsActive,
            IsBlocked = data.IsBlocked,
            MustChangePassword = data.MustChangePassword,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _repositoryManager.User.CreateUser(user);
        await _repositoryManager.SaveAsync(cancellationToken);
        await LogAsync("user", user.Id, "user_created", $"Создан пользователь {user.Username}", cancellationToken);
    }

    public async Task UpdateUserAsync(AdminUserEditData data, CancellationToken cancellationToken = default)
    {
        ValidateUser(data, isCreate: false);

        uint userId = data.Id ?? throw new InvalidOperationException("Не выбран пользователь для изменения.");
        User user = await _repositoryManager.User.GetUserByIdWithRoleAsync(userId, trackChanges: true, cancellationToken)
            ?? throw new InvalidOperationException("Пользователь не найден.");

        string username = data.Username.Trim();
        string? email = InputValidationHelper.NormalizeOptionalEmail(data.Email);

        if (await _repositoryManager.User.ExistsByUsernameExceptIdAsync(username, userId, cancellationToken))
        {
            throw new InvalidOperationException("Пользователь с таким логином уже существует.");
        }

        if (!string.IsNullOrWhiteSpace(email)
            && await _repositoryManager.User.ExistsByEmailExceptIdAsync(email, userId, cancellationToken))
        {
            throw new InvalidOperationException("Пользователь с такой почтой уже существует.");
        }

        user.Username = username;
        user.FullName = data.FullName.Trim();
        user.RoleId = data.RoleId;
        user.Email = email;
        user.Phone = InputValidationHelper.NormalizeOptionalPhone(data.Phone);
        user.CompanyName = NormalizeOptional(data.CompanyName);
        user.IsActive = data.IsActive;
        user.IsBlocked = data.IsBlocked;
        user.MustChangePassword = data.MustChangePassword;
        user.UpdatedAt = DateTime.Now;

        if (!string.IsNullOrWhiteSpace(data.Password))
        {
            user.PasswordHash = _passwordHasher.HashPassword(data.Password);
        }

        _repositoryManager.User.UpdateUser(user);
        await _repositoryManager.SaveAsync(cancellationToken);
        await LogAsync("user", user.Id, "user_updated", $"Обновлен пользователь {user.Username}", cancellationToken);
    }

    public async Task DeleteUserAsync(uint userId, CancellationToken cancellationToken = default)
    {
        if (_authStateService.CurrentUser?.Id == userId)
        {
            throw new InvalidOperationException("Нельзя удалить текущего пользователя.");
        }

        User user = await _repositoryManager.User.GetUserByIdWithRoleAsync(userId, trackChanges: true, cancellationToken)
            ?? throw new InvalidOperationException("Пользователь не найден.");

        string username = user.Username;
        _repositoryManager.User.DeleteUser(user);
        await _repositoryManager.SaveAsync(cancellationToken);
        await LogAsync("user", userId, "user_deleted", $"Удален пользователь {username}", cancellationToken);
    }

    public async Task CreateCargoAsync(AdminCargoEditData data, CancellationToken cancellationToken = default)
    {
        ValidateCargo(data);

        var cargo = new CargoItem
        {
            Name = data.Name.Trim(),
            CargoType = data.CargoType.Trim(),
            WeightKg = data.WeightKg,
            VolumeM3 = data.VolumeM3,
            Description = NormalizeOptional(data.Description),
            SpecialRequirements = NormalizeOptional(data.SpecialRequirements),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _repositoryManager.Cargo.CreateCargo(cargo);
        await _repositoryManager.SaveAsync(cancellationToken);
        await LogAsync("cargo", cargo.Id, "cargo_created", $"Добавлен груз {cargo.Name}", cancellationToken);
    }

    public async Task UpdateCargoAsync(AdminCargoEditData data, CancellationToken cancellationToken = default)
    {
        ValidateCargo(data);

        uint cargoId = data.Id ?? throw new InvalidOperationException("Не выбран груз для изменения.");
        CargoItem cargo = await _repositoryManager.Cargo.GetCargoByIdAsync(cargoId, trackChanges: true, cancellationToken)
            ?? throw new InvalidOperationException("Груз не найден.");

        cargo.Name = data.Name.Trim();
        cargo.CargoType = data.CargoType.Trim();
        cargo.WeightKg = data.WeightKg;
        cargo.VolumeM3 = data.VolumeM3;
        cargo.Description = NormalizeOptional(data.Description);
        cargo.SpecialRequirements = NormalizeOptional(data.SpecialRequirements);
        cargo.UpdatedAt = DateTime.Now;

        _repositoryManager.Cargo.UpdateCargo(cargo);
        await _repositoryManager.SaveAsync(cancellationToken);
        await LogAsync("cargo", cargo.Id, "cargo_updated", $"Обновлен груз {cargo.Name}", cancellationToken);
    }

    public async Task DeleteCargoAsync(uint cargoId, CancellationToken cancellationToken = default)
    {
        CargoItem cargo = await _repositoryManager.Cargo.GetCargoByIdAsync(cargoId, trackChanges: true, cancellationToken)
            ?? throw new InvalidOperationException("Груз не найден.");

        string cargoName = cargo.Name;
        _repositoryManager.Cargo.DeleteCargo(cargo);
        await _repositoryManager.SaveAsync(cancellationToken);
        await LogAsync("cargo", cargoId, "cargo_deleted", $"Удален груз {cargoName}", cancellationToken);
    }

    public async Task CreateDriverAsync(AdminDriverEditData data, CancellationToken cancellationToken = default)
    {
        ValidateDriver(data);

        if (await _repositoryManager.Driver.ExistsByUserIdExceptIdAsync(data.UserId, 0, cancellationToken))
        {
            throw new InvalidOperationException("Для выбранного пользователя уже создана карточка водителя.");
        }

        if (await _repositoryManager.Driver.ExistsByLicenseNumberExceptIdAsync(data.LicenseNumber.Trim(), 0, cancellationToken))
        {
            throw new InvalidOperationException("Водитель с таким номером ВУ уже существует.");
        }

        var driver = new Driver
        {
            UserId = data.UserId,
            LicenseNumber = data.LicenseNumber.Trim(),
            LicenseCategory = data.LicenseCategory.Trim(),
            ExperienceYears = data.ExperienceYears,
            Status = data.Status,
            Notes = NormalizeOptional(data.Notes),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _repositoryManager.Driver.CreateDriver(driver);
        await _repositoryManager.SaveAsync(cancellationToken);
        await LogAsync("driver", driver.Id, "driver_created", $"Создана карточка водителя {driver.LicenseNumber}", cancellationToken);
    }

    public async Task UpdateDriverAsync(AdminDriverEditData data, CancellationToken cancellationToken = default)
    {
        ValidateDriver(data);

        uint driverId = data.Id ?? throw new InvalidOperationException("Не выбран водитель для изменения.");
        Driver driver = await _repositoryManager.Driver.GetDriverByIdWithUserAsync(driverId, trackChanges: true, cancellationToken)
            ?? throw new InvalidOperationException("Водитель не найден.");

        if (await _repositoryManager.Driver.ExistsByUserIdExceptIdAsync(data.UserId, driverId, cancellationToken))
        {
            throw new InvalidOperationException("Для выбранного пользователя уже создана карточка водителя.");
        }

        if (await _repositoryManager.Driver.ExistsByLicenseNumberExceptIdAsync(data.LicenseNumber.Trim(), driverId, cancellationToken))
        {
            throw new InvalidOperationException("Водитель с таким номером ВУ уже существует.");
        }

        driver.UserId = data.UserId;
        driver.LicenseNumber = data.LicenseNumber.Trim();
        driver.LicenseCategory = data.LicenseCategory.Trim();
        driver.ExperienceYears = data.ExperienceYears;
        driver.Status = data.Status;
        driver.Notes = NormalizeOptional(data.Notes);
        driver.UpdatedAt = DateTime.Now;

        _repositoryManager.Driver.UpdateDriver(driver);
        await _repositoryManager.SaveAsync(cancellationToken);
        await LogAsync("driver", driver.Id, "driver_updated", $"Обновлена карточка водителя {driver.LicenseNumber}", cancellationToken);
    }

    public async Task DeleteDriverAsync(uint driverId, CancellationToken cancellationToken = default)
    {
        Driver driver = await _repositoryManager.Driver.GetDriverByIdWithUserAsync(driverId, trackChanges: true, cancellationToken)
            ?? throw new InvalidOperationException("Водитель не найден.");

        string licenseNumber = driver.LicenseNumber;
        _repositoryManager.Driver.DeleteDriver(driver);
        await _repositoryManager.SaveAsync(cancellationToken);
        await LogAsync("driver", driverId, "driver_deleted", $"Удалена карточка водителя {licenseNumber}", cancellationToken);
    }

    public async Task CreateVehicleAsync(AdminVehicleEditData data, CancellationToken cancellationToken = default)
    {
        ValidateVehicle(data);

        if (await _repositoryManager.Vehicle.ExistsByLicensePlateExceptIdAsync(data.LicensePlate.Trim(), 0, cancellationToken))
        {
            throw new InvalidOperationException("Транспорт с таким госномером уже существует.");
        }

        await EnsureDriverCanBeAttachedToVehicleAsync(data.CurrentDriverId, 0, cancellationToken);

        var vehicle = new Vehicle
        {
            Model = data.Model.Trim(),
            LicensePlate = data.LicensePlate.Trim().ToUpperInvariant(),
            CapacityKg = data.CapacityKg,
            VolumeM3 = data.VolumeM3,
            BodyType = NormalizeOptional(data.BodyType),
            ProductionYear = data.ProductionYear,
            Status = data.Status,
            InsuranceExpiry = ToDateOnly(data.InsuranceExpiry),
            CurrentDriverId = data.CurrentDriverId,
            Notes = NormalizeOptional(data.Notes),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _repositoryManager.Vehicle.CreateVehicle(vehicle);
        await _repositoryManager.SaveAsync(cancellationToken);
        await LogAsync("vehicle", vehicle.Id, "vehicle_created", $"Добавлен транспорт {vehicle.LicensePlate}", cancellationToken);
    }

    public async Task UpdateVehicleAsync(AdminVehicleEditData data, CancellationToken cancellationToken = default)
    {
        ValidateVehicle(data);

        uint vehicleId = data.Id ?? throw new InvalidOperationException("Не выбран транспорт для изменения.");
        Vehicle vehicle = await _repositoryManager.Vehicle.GetVehicleByIdWithDriverAsync(vehicleId, trackChanges: true, cancellationToken)
            ?? throw new InvalidOperationException("Транспорт не найден.");

        if (await _repositoryManager.Vehicle.ExistsByLicensePlateExceptIdAsync(data.LicensePlate.Trim(), vehicleId, cancellationToken))
        {
            throw new InvalidOperationException("Транспорт с таким госномером уже существует.");
        }

        await EnsureDriverCanBeAttachedToVehicleAsync(data.CurrentDriverId, vehicleId, cancellationToken);

        vehicle.Model = data.Model.Trim();
        vehicle.LicensePlate = data.LicensePlate.Trim().ToUpperInvariant();
        vehicle.CapacityKg = data.CapacityKg;
        vehicle.VolumeM3 = data.VolumeM3;
        vehicle.BodyType = NormalizeOptional(data.BodyType);
        vehicle.ProductionYear = data.ProductionYear;
        vehicle.Status = data.Status;
        vehicle.InsuranceExpiry = ToDateOnly(data.InsuranceExpiry);
        vehicle.CurrentDriverId = data.CurrentDriverId;
        vehicle.Notes = NormalizeOptional(data.Notes);
        vehicle.UpdatedAt = DateTime.Now;

        _repositoryManager.Vehicle.UpdateVehicle(vehicle);
        await _repositoryManager.SaveAsync(cancellationToken);
        await LogAsync("vehicle", vehicle.Id, "vehicle_updated", $"Обновлен транспорт {vehicle.LicensePlate}", cancellationToken);
    }

    public async Task DeleteVehicleAsync(uint vehicleId, CancellationToken cancellationToken = default)
    {
        Vehicle vehicle = await _repositoryManager.Vehicle.GetVehicleByIdWithDriverAsync(vehicleId, trackChanges: true, cancellationToken)
            ?? throw new InvalidOperationException("Транспорт не найден.");

        string licensePlate = vehicle.LicensePlate;
        _repositoryManager.Vehicle.DeleteVehicle(vehicle);
        await _repositoryManager.SaveAsync(cancellationToken);
        await LogAsync("vehicle", vehicleId, "vehicle_deleted", $"Удален транспорт {licensePlate}", cancellationToken);
    }

    public async Task<Order> CreateOrderWithCargoAsync(AdminOrderEditData data, CargoItem cargo, CancellationToken cancellationToken = default)
    {
        _repositoryManager.Cargo.CreateCargo(cargo);
        await _repositoryManager.SaveAsync(cancellationToken);

        var orderData = data with { CargoId = cargo.Id };
        return await CreateOrderAsync(orderData, cancellationToken);
    }

    public async Task<Order> CreateOrderAsync(AdminOrderEditData data, CancellationToken cancellationToken = default)
    {
        ValidateOrder(data);

        string orderNumber = string.IsNullOrWhiteSpace(data.OrderNumber)
            ? GenerateOrderNumber()
            : data.OrderNumber.Trim().ToUpperInvariant();

        if (await _repositoryManager.Order.ExistsByOrderNumberExceptIdAsync(orderNumber, 0, cancellationToken))
        {
            throw new InvalidOperationException("Заказ с таким номером уже существует.");
        }

        var order = new Order
        {
            OrderNumber = orderNumber,
            ReceiverUserId = data.ReceiverUserId,
            CargoId = data.CargoId,
            DriverId = data.DriverId,
            VehicleId = data.VehicleId,
            PickupAddress = data.PickupAddress.Trim(),
            DeliveryAddress = data.DeliveryAddress.Trim(),
            PickupContactName = NormalizeOptional(data.PickupContactName),
            PickupContactPhone = InputValidationHelper.NormalizeOptionalPhone(data.PickupContactPhone),
            DeliveryContactName = NormalizeOptional(data.DeliveryContactName),
            DeliveryContactPhone = InputValidationHelper.NormalizeOptionalPhone(data.DeliveryContactPhone),
            DistanceKm = data.DistanceKm,
            TotalCost = data.TotalCost,
            Status = data.Status,
            PlannedPickupAt = data.PlannedPickupAt,
            DesiredDeliveryAt = data.DesiredDeliveryAt,
            CancellationReason = NormalizeOptional(data.CancellationReason),
            CreatedByUserId = GetCurrentUserId(),
            Comment = NormalizeOptional(data.Comment),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _repositoryManager.Order.CreateOrder(order);
        await _repositoryManager.SaveAsync(cancellationToken);

        _repositoryManager.OrderStatusHistory.CreateOrderStatusHistory(new OrderStatusHistory
        {
            OrderId = order.Id,
            NewStatus = order.Status,
            ChangedByUserId = GetCurrentUserId(),
            ChangedAt = DateTime.Now,
            Comment = GetOrderCreatedHistoryComment()
        });

        await CreateOrderNotificationsAsync(order, cancellationToken);
        await _repositoryManager.SaveAsync(cancellationToken);
        await LogAsync("order", order.Id, "order_created", $"Создан заказ {order.OrderNumber}", cancellationToken);
        return order;
    }

    public async Task UpdateOrderAsync(AdminOrderEditData data, CancellationToken cancellationToken = default)
    {
        ValidateOrder(data);

        uint orderId = data.Id ?? throw new InvalidOperationException("Не выбран заказ для изменения.");
        Order order = await _repositoryManager.Order.GetOrderByIdDetailedAsync(orderId, trackChanges: true, cancellationToken)
            ?? throw new InvalidOperationException("Заказ не найден.");

        string orderNumber = string.IsNullOrWhiteSpace(data.OrderNumber)
            ? order.OrderNumber
            : data.OrderNumber.Trim().ToUpperInvariant();

        if (await _repositoryManager.Order.ExistsByOrderNumberExceptIdAsync(orderNumber, orderId, cancellationToken))
        {
            throw new InvalidOperationException("Заказ с таким номером уже существует.");
        }

        string oldStatus = order.Status;
        order.OrderNumber = orderNumber;
        order.ReceiverUserId = data.ReceiverUserId;
        order.CargoId = data.CargoId;
        order.DriverId = data.DriverId;
        order.VehicleId = data.VehicleId;
        order.PickupAddress = data.PickupAddress.Trim();
        order.DeliveryAddress = data.DeliveryAddress.Trim();
        order.PickupContactName = NormalizeOptional(data.PickupContactName);
        order.PickupContactPhone = InputValidationHelper.NormalizeOptionalPhone(data.PickupContactPhone);
        order.DeliveryContactName = NormalizeOptional(data.DeliveryContactName);
        order.DeliveryContactPhone = InputValidationHelper.NormalizeOptionalPhone(data.DeliveryContactPhone);
        order.DistanceKm = data.DistanceKm;
        order.TotalCost = data.TotalCost;
        order.Status = data.Status;
        order.PlannedPickupAt = data.PlannedPickupAt;
        order.DesiredDeliveryAt = data.DesiredDeliveryAt;
        order.CancellationReason = NormalizeOptional(data.CancellationReason);
        order.Comment = NormalizeOptional(data.Comment);
        order.UpdatedAt = DateTime.Now;

        if (oldStatus != order.Status)
        {
            _repositoryManager.OrderStatusHistory.CreateOrderStatusHistory(new OrderStatusHistory
            {
                OrderId = order.Id,
                OldStatus = oldStatus,
                NewStatus = order.Status,
                ChangedByUserId = GetCurrentUserId(),
                ChangedAt = DateTime.Now,
                Comment = "Статус изменен администратором"
            });
        }

        _repositoryManager.Order.UpdateOrder(order);
        await _repositoryManager.SaveAsync(cancellationToken);
        await LogAsync("order", order.Id, "order_updated", $"Обновлен заказ {order.OrderNumber}", cancellationToken);
    }

    public async Task DeleteOrderAsync(uint orderId, CancellationToken cancellationToken = default)
    {
        Order order = await _repositoryManager.Order.GetOrderByIdDetailedAsync(orderId, trackChanges: true, cancellationToken)
            ?? throw new InvalidOperationException("Заказ не найден.");

        string orderNumber = order.OrderNumber;
        _repositoryManager.Order.DeleteOrder(order);
        await _repositoryManager.SaveAsync(cancellationToken);
        await LogAsync("order", orderId, "order_deleted", $"Удален заказ {orderNumber}", cancellationToken);
    }

    private async Task EnsureDriverCanBeAttachedToVehicleAsync(uint? driverId, uint vehicleId, CancellationToken cancellationToken)
    {
        if (driverId.HasValue
            && await _repositoryManager.Vehicle.ExistsByCurrentDriverIdExceptIdAsync(driverId.Value, vehicleId, cancellationToken))
        {
            throw new InvalidOperationException("Этот водитель уже закреплен за другим транспортом.");
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

    private async Task CreateOrderNotificationsAsync(Order order, CancellationToken cancellationToken)
    {
        DateTime now = DateTime.Now;
        _repositoryManager.Notification.CreateNotification(new Notification
        {
            UserId = order.ReceiverUserId,
            Title = $"Заказ {order.OrderNumber} создан",
            Message = "Заявка на перевозку создана и ожидает обработки диспетчером.",
            NotificationType = "order",
            IsRead = false,
            CreatedAt = now
        });

        if (order.DriverId.HasValue)
        {
            Driver? driver = await _repositoryManager.Driver.GetDriverByIdWithUserAsync(order.DriverId.Value, trackChanges: false, cancellationToken);
            if (driver is not null)
            {
                _repositoryManager.Notification.CreateNotification(new Notification
                {
                    UserId = driver.UserId,
                    Title = $"Назначен заказ {order.OrderNumber}",
                    Message = "Вам назначен новый рейс. Проверьте детали маршрута в разделе «Мои рейсы».",
                    NotificationType = "order",
                    IsRead = false,
                    CreatedAt = now
                });
            }
        }
    }

    private string GetOrderCreatedHistoryComment()
    {
        string roleCode = _authStateService.CurrentUser?.RoleCode ?? string.Empty;

        return roleCode switch
        {
            "receiver" => "Заказ создан получателем",
            "dispatcher" => "Заказ создан диспетчером",
            _ => "Заказ создан администратором"
        };
    }

    private uint GetCurrentUserId() =>
        _authStateService.CurrentUser?.Id
        ?? throw new InvalidOperationException("Не удалось определить текущего пользователя.");

    private static void ValidateUser(AdminUserEditData data, bool isCreate)
    {
        if (string.IsNullOrWhiteSpace(data.Username))
        {
            throw new InvalidOperationException("Укажите логин пользователя.");
        }

        if (string.IsNullOrWhiteSpace(data.FullName))
        {
            throw new InvalidOperationException("Укажите ФИО или название пользователя.");
        }

        if (data.RoleId == 0)
        {
            throw new InvalidOperationException("Выберите роль пользователя.");
        }

        if (isCreate && string.IsNullOrWhiteSpace(data.Password))
        {
            throw new InvalidOperationException("Для нового пользователя нужно указать пароль.");
        }

        if (string.IsNullOrWhiteSpace(data.Email))
        {
            throw new InvalidOperationException("Укажите email.");
        }

        if (!InputValidationHelper.IsValidAsciiEmail(data.Email))
        {
            throw new InvalidOperationException("Email должен быть записан латиницей и содержать символ @.");
        }

        if (string.IsNullOrWhiteSpace(data.Phone))
        {
            throw new InvalidOperationException("Укажите телефон.");
        }

        if (InputValidationHelper.KeepDigitsOnly(data.Phone) != data.Phone.Trim())
        {
            throw new InvalidOperationException("Телефон должен содержать только цифры.");
        }
    }

    private static void ValidateDriver(AdminDriverEditData data)
    {
        if (data.UserId == 0)
        {
            throw new InvalidOperationException("Выберите пользователя с ролью водителя.");
        }

        if (string.IsNullOrWhiteSpace(data.LicenseNumber))
        {
            throw new InvalidOperationException("Укажите номер водительского удостоверения.");
        }

        if (string.IsNullOrWhiteSpace(data.LicenseCategory))
        {
            throw new InvalidOperationException("Укажите категорию ВУ.");
        }
    }

    private static void ValidateVehicle(AdminVehicleEditData data)
    {
        if (string.IsNullOrWhiteSpace(data.Model))
        {
            throw new InvalidOperationException("Укажите модель транспорта.");
        }

        if (string.IsNullOrWhiteSpace(data.LicensePlate))
        {
            throw new InvalidOperationException("Укажите госномер транспорта.");
        }

        if (data.CapacityKg <= 0)
        {
            throw new InvalidOperationException("Грузоподъемность должна быть больше нуля.");
        }
    }

    private static void ValidateCargo(AdminCargoEditData data)
    {
        if (string.IsNullOrWhiteSpace(data.Name))
        {
            throw new InvalidOperationException("Укажите наименование груза.");
        }

        if (string.IsNullOrWhiteSpace(data.CargoType))
        {
            throw new InvalidOperationException("Выберите тип груза.");
        }

        if (data.WeightKg <= 0)
        {
            throw new InvalidOperationException("Вес груза должен быть больше нуля.");
        }
    }

    private static void ValidateOrder(AdminOrderEditData data)
    {
        if (data.ReceiverUserId == 0)
        {
            throw new InvalidOperationException("Выберите получателя.");
        }

        if (data.CargoId == 0)
        {
            throw new InvalidOperationException("Выберите груз.");
        }

        if (string.IsNullOrWhiteSpace(data.PickupAddress))
        {
            throw new InvalidOperationException("Укажите адрес погрузки.");
        }

        if (string.IsNullOrWhiteSpace(data.DeliveryAddress))
        {
            throw new InvalidOperationException("Укажите адрес доставки.");
        }

        if (data.PlannedPickupAt.HasValue && data.PlannedPickupAt.Value.Date < DateTime.Today)
        {
            throw new InvalidOperationException("Дата погрузки не может быть раньше сегодняшнего дня.");
        }

        if (data.DesiredDeliveryAt.HasValue && data.DesiredDeliveryAt.Value.Date < DateTime.Today)
        {
            throw new InvalidOperationException("Дата доставки не может быть раньше сегодняшнего дня.");
        }

        if (data.Status is "assigned" or "accepted" or "loading" or "in_transit" or "delivered" or "received"
            && (!data.DriverId.HasValue || !data.VehicleId.HasValue))
        {
            throw new InvalidOperationException("Для выбранного статуса нужно назначить водителя и транспорт.");
        }

        if (data.Status == "cancelled" && string.IsNullOrWhiteSpace(data.CancellationReason))
        {
            throw new InvalidOperationException("Для отмены заказа укажите причину.");
        }
    }

    private static string GenerateOrderNumber() => $"TRK-{DateTime.Now:yyyyMMdd-HHmmss}";

    private static DateOnly? ToDateOnly(DateTime? value) =>
        value.HasValue
            ? DateOnly.FromDateTime(value.Value)
            : null;

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
