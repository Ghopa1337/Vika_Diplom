using CargoTransport.Desktop.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoTransport.Desktop.Data;

public class CargoTransportDbContext : DbContext
{
    public CargoTransportDbContext(DbContextOptions<CargoTransportDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<CargoItem> CargoItems => Set<CargoItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderRequest> OrderRequests => Set<OrderRequest>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<PasswordHistory> PasswordHistory => Set<PasswordHistory>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id").HasColumnType("int unsigned").ValueGeneratedOnAdd();
            entity.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description");

            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id").HasColumnType("int unsigned").ValueGeneratedOnAdd();
            entity.Property(x => x.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
            entity.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(255).IsRequired();
            entity.Property(x => x.RoleId).HasColumnName("role_id").HasColumnType("int unsigned").IsRequired();
            entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
            entity.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(20);
            entity.Property(x => x.CompanyName).HasColumnName("company_name").HasMaxLength(255);
            entity.Property(x => x.IsBlocked).HasColumnName("is_blocked");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.MustChangePassword).HasColumnName("must_change_password");
            entity.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();

            entity.HasOne(x => x.Role)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.ToTable("drivers");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id").HasColumnType("int unsigned").ValueGeneratedOnAdd();
            entity.Property(x => x.UserId).HasColumnName("user_id").HasColumnType("int unsigned").IsRequired();
            entity.Property(x => x.LicenseNumber).HasColumnName("license_number").HasMaxLength(50).IsRequired();
            entity.Property(x => x.LicenseCategory).HasColumnName("license_category").HasMaxLength(20).IsRequired();
            entity.Property(x => x.ExperienceYears).HasColumnName("experience_years").HasColumnType("smallint unsigned");
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
            entity.Property(x => x.Rating).HasColumnName("rating").HasPrecision(3, 2);
            entity.Property(x => x.CurrentLatitude).HasColumnName("current_latitude").HasPrecision(10, 8);
            entity.Property(x => x.CurrentLongitude).HasColumnName("current_longitude").HasPrecision(11, 8);
            entity.Property(x => x.Notes).HasColumnName("notes");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(x => x.UserId).IsUnique();
            entity.HasIndex(x => x.LicenseNumber).IsUnique();
            entity.HasIndex(x => x.Status);

            entity.HasOne(x => x.User)
                .WithOne(x => x.DriverProfile)
                .HasForeignKey<Driver>(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.ToTable("vehicles");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id").HasColumnType("int unsigned").ValueGeneratedOnAdd();
            entity.Property(x => x.Model).HasColumnName("model").HasMaxLength(100).IsRequired();
            entity.Property(x => x.LicensePlate).HasColumnName("license_plate").HasMaxLength(20).IsRequired();
            entity.Property(x => x.CapacityKg).HasColumnName("capacity_kg").HasPrecision(10, 2);
            entity.Property(x => x.VolumeM3).HasColumnName("volume_m3").HasPrecision(10, 2);
            entity.Property(x => x.BodyType).HasColumnName("body_type").HasMaxLength(50);
            entity.Property(x => x.ProductionYear).HasColumnName("production_year").HasColumnType("smallint unsigned");
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
            entity.Property(x => x.InsuranceExpiry).HasColumnName("insurance_expiry");
            entity.Property(x => x.CurrentDriverId).HasColumnName("current_driver_id").HasColumnType("int unsigned");
            entity.Property(x => x.Notes).HasColumnName("notes");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(x => x.LicensePlate).IsUnique();
            entity.HasIndex(x => x.CurrentDriverId).IsUnique();
            entity.HasIndex(x => x.Status);

            entity.HasOne(x => x.CurrentDriver)
                .WithOne(x => x.CurrentVehicle)
                .HasForeignKey<Vehicle>(x => x.CurrentDriverId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CargoItem>(entity =>
        {
            entity.ToTable("cargo");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id").HasColumnType("int unsigned").ValueGeneratedOnAdd();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(x => x.CargoType).HasColumnName("cargo_type").HasMaxLength(50).IsRequired();
            entity.Property(x => x.WeightKg).HasColumnName("weight_kg").HasPrecision(10, 2);
            entity.Property(x => x.VolumeM3).HasColumnName("volume_m3").HasPrecision(10, 2);
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.SpecialRequirements).HasColumnName("special_requirements");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(x => x.CargoType);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id").HasColumnType("int unsigned").ValueGeneratedOnAdd();
            entity.Property(x => x.OrderNumber).HasColumnName("order_number").HasMaxLength(50).IsRequired();
            entity.Property(x => x.ReceiverUserId).HasColumnName("receiver_user_id").HasColumnType("int unsigned").IsRequired();
            entity.Property(x => x.CargoId).HasColumnName("cargo_id").HasColumnType("int unsigned").IsRequired();
            entity.Property(x => x.DriverId).HasColumnName("driver_id").HasColumnType("int unsigned");
            entity.Property(x => x.VehicleId).HasColumnName("vehicle_id").HasColumnType("int unsigned");
            entity.Property(x => x.PickupAddress).HasColumnName("pickup_address").IsRequired();
            entity.Property(x => x.DeliveryAddress).HasColumnName("delivery_address").IsRequired();
            entity.Property(x => x.PickupContactName).HasColumnName("pickup_contact_name").HasMaxLength(255);
            entity.Property(x => x.PickupContactPhone).HasColumnName("pickup_contact_phone").HasMaxLength(20);
            entity.Property(x => x.DeliveryContactName).HasColumnName("delivery_contact_name").HasMaxLength(255);
            entity.Property(x => x.DeliveryContactPhone).HasColumnName("delivery_contact_phone").HasMaxLength(20);
            entity.Property(x => x.DistanceKm).HasColumnName("distance_km").HasPrecision(10, 2);
            entity.Property(x => x.TotalCost).HasColumnName("total_cost").HasPrecision(15, 2);
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
            entity.Property(x => x.PlannedPickupAt).HasColumnName("planned_pickup_at");
            entity.Property(x => x.DesiredDeliveryAt).HasColumnName("desired_delivery_at");
            entity.Property(x => x.ActualPickupAt).HasColumnName("actual_pickup_at");
            entity.Property(x => x.ActualDeliveryAt).HasColumnName("actual_delivery_at");
            entity.Property(x => x.ReceivedAt).HasColumnName("received_at");
            entity.Property(x => x.CancellationReason).HasColumnName("cancellation_reason");
            entity.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("int unsigned").IsRequired();
            entity.Property(x => x.Comment).HasColumnName("comment");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(x => x.OrderNumber).IsUnique();
            entity.HasIndex(x => x.ReceiverUserId);
            entity.HasIndex(x => x.CargoId);
            entity.HasIndex(x => x.DriverId);
            entity.HasIndex(x => x.VehicleId);
            entity.HasIndex(x => x.CreatedByUserId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.DesiredDeliveryAt);

            entity.HasOne(x => x.ReceiverUser)
                .WithMany(x => x.ReceiverOrders)
                .HasForeignKey(x => x.ReceiverUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Cargo)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.CargoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Driver)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.DriverId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.Vehicle)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.VehicleId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.CreatedByUser)
                .WithMany(x => x.CreatedOrders)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderRequest>(entity =>
        {
            entity.ToTable("order_requests");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id").HasColumnType("int unsigned").ValueGeneratedOnAdd();
            entity.Property(x => x.ReceiverUserId).HasColumnName("receiver_user_id").HasColumnType("int unsigned").IsRequired();
            entity.Property(x => x.CargoDescription).HasColumnName("cargo_description").HasMaxLength(500).IsRequired();
            entity.Property(x => x.PickupAddress).HasColumnName("pickup_address").HasMaxLength(500).IsRequired();
            entity.Property(x => x.DeliveryAddress).HasColumnName("delivery_address").HasMaxLength(500).IsRequired();
            entity.Property(x => x.PickupContactPhone).HasColumnName("pickup_contact_phone").HasMaxLength(20).IsRequired();
            entity.Property(x => x.DeliveryContactPhone).HasColumnName("delivery_contact_phone").HasMaxLength(20).IsRequired();
            entity.Property(x => x.DesiredDate).HasColumnName("desired_date");
            entity.Property(x => x.Comment).HasColumnName("comment");
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
            entity.Property(x => x.ProcessedByUserId).HasColumnName("processed_by_user_id").HasColumnType("int unsigned");
            entity.Property(x => x.CreatedOrderId).HasColumnName("created_order_id").HasColumnType("int unsigned");
            entity.Property(x => x.ProcessedAt).HasColumnName("processed_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(x => x.ReceiverUserId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.ProcessedByUserId);
            entity.HasIndex(x => x.CreatedOrderId).IsUnique();

            entity.HasOne(x => x.ReceiverUser)
                .WithMany(x => x.OrderRequests)
                .HasForeignKey(x => x.ReceiverUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ProcessedByUser)
                .WithMany(x => x.ProcessedOrderRequests)
                .HasForeignKey(x => x.ProcessedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.CreatedOrder)
                .WithMany()
                .HasForeignKey(x => x.CreatedOrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.ToTable("order_status_history");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id").HasColumnType("int unsigned").ValueGeneratedOnAdd();
            entity.Property(x => x.OrderId).HasColumnName("order_id").HasColumnType("int unsigned").IsRequired();
            entity.Property(x => x.OldStatus).HasColumnName("old_status").HasMaxLength(30);
            entity.Property(x => x.NewStatus).HasColumnName("new_status").HasMaxLength(30).IsRequired();
            entity.Property(x => x.ChangedByUserId).HasColumnName("changed_by_user_id").HasColumnType("int unsigned").IsRequired();
            entity.Property(x => x.ChangedAt).HasColumnName("changed_at");
            entity.Property(x => x.Comment).HasColumnName("comment");

            entity.HasIndex(x => x.OrderId);
            entity.HasIndex(x => x.ChangedByUserId);
            entity.HasIndex(x => x.ChangedAt);

            entity.HasOne(x => x.Order)
                .WithMany(x => x.StatusHistory)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ChangedByUser)
                .WithMany(x => x.OrderStatusChanges)
                .HasForeignKey(x => x.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id").HasColumnType("int unsigned").ValueGeneratedOnAdd();
            entity.Property(x => x.UserId).HasColumnName("user_id").HasColumnType("int unsigned").IsRequired();
            entity.Property(x => x.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
            entity.Property(x => x.Message).HasColumnName("message").IsRequired();
            entity.Property(x => x.NotificationType).HasColumnName("notification_type").HasMaxLength(50).IsRequired();
            entity.Property(x => x.IsRead).HasColumnName("is_read");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.ReadAt).HasColumnName("read_at");

            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.IsRead);
            entity.HasIndex(x => x.CreatedAt);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.ToTable("reports");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id").HasColumnType("int unsigned").ValueGeneratedOnAdd();
            entity.Property(x => x.ReportType).HasColumnName("report_type").HasMaxLength(50).IsRequired();
            entity.Property(x => x.ReportData).HasColumnName("report_data").HasColumnType("json").IsRequired();
            entity.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("int unsigned");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(x => x.ReportType);
            entity.HasIndex(x => x.CreatedByUserId);
            entity.HasIndex(x => x.CreatedAt);

            entity.HasOne(x => x.CreatedByUser)
                .WithMany(x => x.Reports)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PasswordHistory>(entity =>
        {
            entity.ToTable("password_history");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id").HasColumnType("int unsigned").ValueGeneratedOnAdd();
            entity.Property(x => x.UserId).HasColumnName("user_id").HasColumnType("int unsigned").IsRequired();
            entity.Property(x => x.OldPasswordHash).HasColumnName("old_password_hash").HasMaxLength(255).IsRequired();
            entity.Property(x => x.ChangedAt).HasColumnName("changed_at");

            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.ChangedAt);

            entity.HasOne(x => x.User)
                .WithMany(x => x.PasswordHistoryEntries)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.ToTable("activity_logs");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id").HasColumnType("int unsigned").ValueGeneratedOnAdd();
            entity.Property(x => x.UserId).HasColumnName("user_id").HasColumnType("int unsigned");
            entity.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(50).IsRequired();
            entity.Property(x => x.EntityId).HasColumnName("entity_id").HasColumnType("int unsigned");
            entity.Property(x => x.ActionCode).HasColumnName("action_code").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => new { x.EntityType, x.EntityId });
            entity.HasIndex(x => x.CreatedAt);

            entity.HasOne(x => x.User)
                .WithMany(x => x.ActivityLogs)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
