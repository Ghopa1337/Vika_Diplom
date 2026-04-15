using CargoTransport.Desktop.Models;
using Microsoft.Extensions.Options;
using Sieve.Models;
using Sieve.Services;

namespace CargoTransport.Desktop.Services;

public sealed class CargoSieveProcessor : SieveProcessor
{
    public CargoSieveProcessor(
        IOptions<SieveOptions> options,
        ISieveCustomFilterMethods customFilters)
        : base(options, customFilters)
    {
    }

    protected override SievePropertyMapper MapProperties(SievePropertyMapper mapper)
    {
        mapper.Property<User>(x => x.Role.Name)
            .CanFilter()
            .CanSort()
            .HasName("role");

        mapper.Property<User>(x => x.Role.Code)
            .CanFilter()
            .CanSort()
            .HasName("roleCode");

        mapper.Property<Driver>(x => x.User.FullName)
            .CanFilter()
            .CanSort()
            .HasName("driverName");

        mapper.Property<Driver>(x => x.User.Username)
            .CanFilter()
            .CanSort()
            .HasName("username");

        mapper.Property<Driver>(x => x.User.Phone)
            .CanFilter()
            .CanSort()
            .HasName("phone");

        mapper.Property<Vehicle>(x => x.CurrentDriver!.User.FullName)
            .CanFilter()
            .CanSort()
            .HasName("driverName");

        mapper.Property<Vehicle>(x => x.CurrentDriver!.LicenseNumber)
            .CanFilter()
            .CanSort()
            .HasName("driverLicense");

        mapper.Property<Order>(x => x.ReceiverUser.CompanyName)
            .CanFilter()
            .CanSort()
            .HasName("receiver");

        mapper.Property<Order>(x => x.ReceiverUser.FullName)
            .CanFilter()
            .CanSort()
            .HasName("receiverName");

        mapper.Property<Order>(x => x.Cargo.Name)
            .CanFilter()
            .CanSort()
            .HasName("cargoName");

        mapper.Property<Order>(x => x.Driver!.User.FullName)
            .CanFilter()
            .CanSort()
            .HasName("driverName");

        mapper.Property<Order>(x => x.Vehicle!.LicensePlate)
            .CanFilter()
            .CanSort()
            .HasName("vehiclePlate");

        return mapper;
    }
}
