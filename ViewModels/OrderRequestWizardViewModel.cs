using CargoTransport.Desktop.Services;

namespace CargoTransport.Desktop.ViewModels;

public sealed class OrderRequestWizardViewModel : ViewModelBase
{
    private readonly IOrderRequestService _orderRequestService;
    private readonly Func<Task> _onCompleted;

    private string _cargoDescription = string.Empty;
    private string _pickupAddress = string.Empty;
    private string _deliveryAddress = string.Empty;
    private string _pickupContactPhone = string.Empty;
    private string _deliveryContactPhone = string.Empty;
    private DateTime? _desiredDate;
    private string _comment = string.Empty;
    private string _statusMessage = "Заполните упрощенную заявку, а диспетчер потом оформит по ней полноценный заказ.";

    public OrderRequestWizardViewModel(
        IOrderRequestService orderRequestService,
        Func<Task> onCompleted)
    {
        _orderRequestService = orderRequestService;
        _onCompleted = onCompleted;
        SubmitCommand = new AsyncRelayCommand(SubmitAsync, CanSubmit);
    }

    public string CargoDescription
    {
        get => _cargoDescription;
        set => Set(ref _cargoDescription, value);
    }

    public string PickupAddress
    {
        get => _pickupAddress;
        set => Set(ref _pickupAddress, value);
    }

    public string DeliveryAddress
    {
        get => _deliveryAddress;
        set => Set(ref _deliveryAddress, value);
    }

    public string PickupContactPhone
    {
        get => _pickupContactPhone;
        set => Set(ref _pickupContactPhone, InputValidationHelper.KeepDigitsOnly(value));
    }

    public string DeliveryContactPhone
    {
        get => _deliveryContactPhone;
        set => Set(ref _deliveryContactPhone, InputValidationHelper.KeepDigitsOnly(value));
    }

    public DateTime? DesiredDate
    {
        get => _desiredDate;
        set => Set(ref _desiredDate, value);
    }

    public string Comment
    {
        get => _comment;
        set => Set(ref _comment, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => Set(ref _statusMessage, value);
    }

    public DateTime MinSelectableDate => DateTime.Today;
    public AsyncRelayCommand SubmitCommand { get; }

    protected override void OnPropertyChanged(string? propertyName)
    {
        base.OnPropertyChanged(propertyName);

        switch (propertyName)
        {
            case nameof(CargoDescription):
            case nameof(PickupAddress):
            case nameof(DeliveryAddress):
            case nameof(PickupContactPhone):
            case nameof(DeliveryContactPhone):
                SubmitCommand.RaiseCanExecuteChanged();
                break;
        }
    }

    private bool CanSubmit() =>
        !string.IsNullOrWhiteSpace(CargoDescription)
        && !string.IsNullOrWhiteSpace(PickupAddress)
        && !string.IsNullOrWhiteSpace(DeliveryAddress)
        && !string.IsNullOrWhiteSpace(PickupContactPhone)
        && !string.IsNullOrWhiteSpace(DeliveryContactPhone);

    private async Task SubmitAsync()
    {
        try
        {
            await _orderRequestService.CreateRequestForCurrentReceiverAsync(new OrderRequestDraftData(
                CargoDescription,
                PickupAddress,
                DeliveryAddress,
                PickupContactPhone,
                DeliveryContactPhone,
                DesiredDate,
                Comment));

            StatusMessage = "Заявка отправлена диспетчеру.";
            await _onCompleted();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка создания заявки: {ex.Message}";
        }
    }
}
