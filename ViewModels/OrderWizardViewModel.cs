using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CargoTransport.Desktop;
using CargoTransport.Desktop.Models;
using CargoTransport.Desktop.Services;

namespace CargoTransport.Desktop.ViewModels;

public sealed class OrderWizardViewModel : ViewModelBase
{
    private readonly IAdminCrudService _adminCrudService;
    private readonly Func<Task> _onCompleted;
    private readonly bool _lockReceiver;
    private readonly bool _allowAssignment;
    private int _currentStep = 1;

    private uint? _selectedReceiverUserId;
    private bool _isNewCargo = true;
    private uint? _selectedExistingCargoId;
    private string _cargoName = string.Empty;
    private string _cargoWeight = string.Empty;
    private string _cargoVolume = string.Empty;
    private string _selectedCargoType = "normal";
    private string _specialRequirements = string.Empty;
    private string _pickupAddress = string.Empty;
    private string _deliveryAddress = string.Empty;
    private string _pickupContactName = string.Empty;
    private string _pickupContactPhone = string.Empty;
    private string _deliveryContactName = string.Empty;
    private string _deliveryContactPhone = string.Empty;
    private DateTime? _plannedPickupAt;
    private DateTime? _desiredDeliveryAt;
    private string _comment = string.Empty;
    private string _distanceKm = string.Empty;
    private string _totalCost = string.Empty;
    private uint? _selectedDriverId;
    private uint? _selectedVehicleId;
    private string _statusMessage = "Заполните шаги мастера и создайте заказ без ручного CRUD.";

    private bool _isUpdatingCalculatedCost;

    public OrderWizardViewModel(
        IAdminCrudService adminCrudService,
        Func<Task> onCompleted,
        uint? initialReceiverUserId = null,
        bool lockReceiver = false,
        bool allowAssignment = true)
    {
        _adminCrudService = adminCrudService;
        _onCompleted = onCompleted;
        _lockReceiver = lockReceiver;
        _allowAssignment = allowAssignment;
        _selectedReceiverUserId = initialReceiverUserId;

        NextCommand = new RelayCommand(GoNext, CanGoNext);
        BackCommand = new RelayCommand(GoBack, CanGoBack);
        CompleteCommand = new AsyncRelayCommand(CompleteAsync, CanComplete);
    }

    public int CurrentStep
    {
        get => _currentStep;
        set
        {
            if (Set(ref _currentStep, value))
            {
                RaiseCommandStatesChanged();
            }
        }
    }

    public uint? SelectedReceiverUserId
    {
        get => _selectedReceiverUserId;
        set => SetWizardField(ref _selectedReceiverUserId, value);
    }

    public bool IsNewCargo
    {
        get => _isNewCargo;
        set => SetWizardField(ref _isNewCargo, value);
    }

    public uint? SelectedExistingCargoId
    {
        get => _selectedExistingCargoId;
        set => SetWizardField(ref _selectedExistingCargoId, value);
    }

    public string CargoName
    {
        get => _cargoName;
        set => SetWizardField(ref _cargoName, value);
    }

    public string CargoWeight
    {
        get => _cargoWeight;
        set => SetWizardField(ref _cargoWeight, value);
    }

    public string CargoVolume
    {
        get => _cargoVolume;
        set => SetWizardField(ref _cargoVolume, value);
    }

    public string SelectedCargoType
    {
        get => _selectedCargoType;
        set => SetWizardField(ref _selectedCargoType, value);
    }

    public string SpecialRequirements
    {
        get => _specialRequirements;
        set => SetWizardField(ref _specialRequirements, value);
    }

    public string PickupAddress
    {
        get => _pickupAddress;
        set => SetWizardField(ref _pickupAddress, value);
    }

    public string DeliveryAddress
    {
        get => _deliveryAddress;
        set => SetWizardField(ref _deliveryAddress, value);
    }

    public string PickupContactName
    {
        get => _pickupContactName;
        set => SetWizardField(ref _pickupContactName, value);
    }

    public string PickupContactPhone
    {
        get => _pickupContactPhone;
        set => SetWizardField(ref _pickupContactPhone, InputValidationHelper.KeepDigitsOnly(value));
    }

    public string DeliveryContactName
    {
        get => _deliveryContactName;
        set => SetWizardField(ref _deliveryContactName, value);
    }

    public string DeliveryContactPhone
    {
        get => _deliveryContactPhone;
        set => SetWizardField(ref _deliveryContactPhone, InputValidationHelper.KeepDigitsOnly(value));
    }

    public DateTime? PlannedPickupAt
    {
        get => _plannedPickupAt;
        set => SetWizardField(ref _plannedPickupAt, value);
    }

    public DateTime? DesiredDeliveryAt
    {
        get => _desiredDeliveryAt;
        set => SetWizardField(ref _desiredDeliveryAt, value);
    }

    public string Comment
    {
        get => _comment;
        set => SetWizardField(ref _comment, value);
    }

    public string DistanceKm
    {
        get => _distanceKm;
        set
        {
            if (SetWizardField(ref _distanceKm, value))
            {
                UpdateCalculatedCost();
            }
        }
    }

    public string TotalCost
    {
        get => _totalCost;
        set => SetWizardField(ref _totalCost, value);
    }

    public uint? SelectedDriverId
    {
        get => _selectedDriverId;
        set => SetWizardField(ref _selectedDriverId, value);
    }

    public uint? SelectedVehicleId
    {
        get => _selectedVehicleId;
        set => SetWizardField(ref _selectedVehicleId, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => Set(ref _statusMessage, value);
    }

    public bool IsReceiverSelectionEnabled => !_lockReceiver;
    public bool IsAssignmentVisible => _allowAssignment;
    public DateTime MinSelectableDate => DateTime.Today;

    public ObservableCollection<AdminLookupItemViewModel> ReceiverOptions { get; } = [];
    public ObservableCollection<AdminLookupItemViewModel> CargoOptions { get; } = [];
    public ObservableCollection<AdminLookupItemViewModel> DriverOptions { get; } = [];
    public ObservableCollection<AdminLookupItemViewModel> VehicleOptions { get; } = [];

    public RelayCommand NextCommand { get; }
    public RelayCommand BackCommand { get; }
    public AsyncRelayCommand CompleteCommand { get; }

    public async Task LoadLookupsAsync(CancellationToken ct = default)
    {
        IReadOnlyList<AdminLookupItemData> receivers = await _adminCrudService.GetReceiverOptionsAsync(ct);
        IReadOnlyList<AdminLookupItemData> cargo = await _adminCrudService.GetCargoOptionsAsync(ct);
        IReadOnlyList<AdminLookupItemData> drivers = await _adminCrudService.GetDriverOptionsAsync(ct);
        IReadOnlyList<AdminLookupItemData> vehicles = await _adminCrudService.GetVehicleOptionsAsync(ct);

        AdminCollectionHelper.ReplaceWith(ReceiverOptions, receivers.Select(x => new AdminLookupItemViewModel(x.Id, x.DisplayName)));
        AdminCollectionHelper.ReplaceWith(CargoOptions, cargo.Select(x => new AdminLookupItemViewModel(x.Id, x.DisplayName)));
        AdminCollectionHelper.ReplaceWith(DriverOptions, AdminLookupHelper.WithEmpty(drivers, "Не назначать водителя"));
        AdminCollectionHelper.ReplaceWith(VehicleOptions, AdminLookupHelper.WithEmpty(vehicles, "Не назначать транспорт"));

        SelectedReceiverUserId = AdminLookupSelectionHelper.NormalizeRequiredSelection(SelectedReceiverUserId, ReceiverOptions);
        SelectedExistingCargoId = AdminLookupSelectionHelper.NormalizeRequiredSelection(SelectedExistingCargoId, CargoOptions);
        SelectedDriverId = AdminLookupSelectionHelper.NormalizeOptionalSelection(SelectedDriverId, DriverOptions);
        SelectedVehicleId = AdminLookupSelectionHelper.NormalizeOptionalSelection(SelectedVehicleId, VehicleOptions);
    }

    private void GoNext()
    {
        if (CurrentStep < 3)
        {
            CurrentStep++;
            StatusMessage = CurrentStep switch
            {
                2 => "Уточните маршрут, телефоны и желаемые даты доставки.",
                3 => "Проверьте расчёт и при необходимости назначьте экипаж.",
                _ => StatusMessage
            };
        }
    }

    private void GoBack()
    {
        if (CurrentStep > 1)
        {
            CurrentStep--;
            StatusMessage = CurrentStep switch
            {
                1 => "Заполните получателя и параметры груза.",
                2 => "Уточните маршрут и телефоны.",
                _ => StatusMessage
            };
        }
    }

    private bool CanGoBack() => CurrentStep > 1;

    private bool CanGoNext() =>
        CurrentStep switch
        {
            1 => CanCompleteCargoStep(),
            2 => !string.IsNullOrWhiteSpace(PickupAddress) && !string.IsNullOrWhiteSpace(DeliveryAddress),
            _ => false
        };

    private bool CanComplete() =>
        CurrentStep == 3
        && SelectedReceiverUserId is > 0
        && CanCompleteCargoStep()
        && !string.IsNullOrWhiteSpace(PickupAddress)
        && !string.IsNullOrWhiteSpace(DeliveryAddress);

    private bool CanCompleteCargoStep()
    {
        if (SelectedReceiverUserId is not > 0)
        {
            return false;
        }

        return IsNewCargo
            ? !string.IsNullOrWhiteSpace(CargoName) && !string.IsNullOrWhiteSpace(CargoWeight)
            : SelectedExistingCargoId is > 0;
    }

    private async Task CompleteAsync()
    {
        if (!TryBuildOrderData(out AdminOrderEditData? orderData, out CargoItem? newCargo, out string? errorMessage))
        {
            StatusMessage = errorMessage ?? "Не удалось подготовить данные заказа.";
            return;
        }

        if (newCargo is not null)
        {
            await _adminCrudService.CreateOrderWithCargoAsync(orderData!, newCargo);
        }
        else
        {
            await _adminCrudService.CreateOrderAsync(orderData!);
        }

        StatusMessage = "Заказ успешно создан.";
        await _onCompleted();
    }

    private bool TryBuildOrderData(out AdminOrderEditData? orderData, out CargoItem? newCargo, out string? errorMessage)
    {
        orderData = null;
        newCargo = null;
        errorMessage = null;

        if (SelectedReceiverUserId is not > 0)
        {
            errorMessage = "Выберите получателя заказа.";
            return false;
        }

        if (!AdminParsingHelper.TryParseNullableDecimal(TotalCost, out decimal? totalCost, out errorMessage))
        {
            return false;
        }

        if (!AdminParsingHelper.TryParseNullableDecimal(DistanceKm, out decimal? distanceKm, out errorMessage))
        {
            return false;
        }

        bool hasDriver = _allowAssignment && SelectedDriverId is > 0;
        bool hasVehicle = _allowAssignment && SelectedVehicleId is > 0;

        if (hasDriver ^ hasVehicle)
        {
            errorMessage = "Для назначения на этапе создания выберите и водителя, и транспорт.";
            return false;
        }

        uint cargoId;
        if (IsNewCargo)
        {
            if (!AdminParsingHelper.TryParseRequiredDecimal(CargoWeight, out decimal weightKg, out errorMessage))
            {
                return false;
            }

            if (!AdminParsingHelper.TryParseNullableDecimal(CargoVolume, out decimal? volumeM3, out errorMessage))
            {
                return false;
            }

            newCargo = new CargoItem
            {
                Name = CargoName.Trim(),
                WeightKg = weightKg,
                VolumeM3 = volumeM3,
                CargoType = SelectedCargoType,
                SpecialRequirements = string.IsNullOrWhiteSpace(SpecialRequirements) ? null : SpecialRequirements.Trim(),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            cargoId = 0;
        }
        else
        {
            if (SelectedExistingCargoId is not > 0)
            {
                errorMessage = "Выберите груз из справочника.";
                return false;
            }

            cargoId = SelectedExistingCargoId.Value;
        }

        string status = hasDriver && hasVehicle
            ? Order.OrderStatuses.Assigned
            : Order.OrderStatuses.Created;

        orderData = new AdminOrderEditData(
            null,
            string.Empty,
            SelectedReceiverUserId.Value,
            cargoId,
            hasDriver ? SelectedDriverId : null,
            hasVehicle ? SelectedVehicleId : null,
            PickupAddress.Trim(),
            DeliveryAddress.Trim(),
            string.IsNullOrWhiteSpace(PickupContactName) ? null : PickupContactName.Trim(),
            string.IsNullOrWhiteSpace(PickupContactPhone) ? null : PickupContactPhone.Trim(),
            string.IsNullOrWhiteSpace(DeliveryContactName) ? null : DeliveryContactName.Trim(),
            string.IsNullOrWhiteSpace(DeliveryContactPhone) ? null : DeliveryContactPhone.Trim(),
            distanceKm,
            totalCost,
            status,
            PlannedPickupAt,
            DesiredDeliveryAt,
            null,
            string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim());

        return true;
    }

    private bool SetWizardField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        bool changed = Set(ref field, value, propertyName);
        if (changed)
        {
            RaiseCommandStatesChanged();
        }

        return changed;
    }

    private void UpdateCalculatedCost()
    {
        if (_isUpdatingCalculatedCost)
        {
            return;
        }

        _isUpdatingCalculatedCost = true;
        try
        {
            if (!AdminParsingHelper.TryParseNullableDecimal(DistanceKm, out decimal? distanceKm, out _))
            {
                TotalCost = string.Empty;
                return;
            }

            TotalCost = distanceKm.HasValue
                ? AdminParsingHelper.FormatDecimal(InputValidationHelper.CalculateDeliveryCost(distanceKm.Value))
                : string.Empty;
        }
        finally
        {
            _isUpdatingCalculatedCost = false;
        }
    }

    private void RaiseCommandStatesChanged()
    {
        NextCommand.RaiseCanExecuteChanged();
        BackCommand.RaiseCanExecuteChanged();
        CompleteCommand.RaiseCanExecuteChanged();
    }
}
