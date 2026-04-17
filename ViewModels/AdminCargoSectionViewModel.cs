using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CargoTransport.Desktop.Services;
using Sieve.Models;

namespace CargoTransport.Desktop.ViewModels;

public sealed class AdminCargoRowViewModel
{
    public required uint Id { get; init; }
    public required string Name { get; init; }
    public required string CargoType { get; init; }
    public required string Weight { get; init; }
    public required string Volume { get; init; }
    public required string UsageCount { get; init; }
    public required string UpdatedAt { get; init; }
    public string? Description { get; init; }
    public string? SpecialRequirements { get; init; }
}

public sealed class AdminCargoSectionViewModel : AdminEditableSectionViewModel
{
    private readonly RelayCommand _beginCreateCommand;
    private readonly AsyncRelayCommand _saveCommand;
    private readonly AsyncRelayCommand _deleteCommand;
    private readonly AsyncRelayCommand _applyFilterCommand;
    private readonly RelayCommand _resetCommand;
    private readonly RelayCommand _clearFilterCommand;
    private AdminCargoRowViewModel? _selectedCargo;
    private uint? _editingCargoId;
    private uint? _deleteArmedCargoId;
    private string _filterSearch = string.Empty;
    private string _filterCargoTypeCode = string.Empty;
    private string _selectedSort = "-UpdatedAt";
    private string _name = string.Empty;
    private string _weightKg = string.Empty;
    private string _volumeM3 = string.Empty;
    private string _selectedCargoTypeCode = "normal";
    private string _description = string.Empty;
    private string _specialRequirements = string.Empty;

    public AdminCargoSectionViewModel(
        IAdminCrudService adminCrudService,
        Func<CancellationToken, Task> refreshPanelAsync)
        : base(adminCrudService, refreshPanelAsync)
    {
        _beginCreateCommand = new RelayCommand(BeginCreate, () => !IsBusy);
        _saveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        _deleteCommand = new AsyncRelayCommand(DeleteAsync, () => !IsBusy && SelectedCargo is not null);
        _applyFilterCommand = new AsyncRelayCommand(RefreshPanelAsync, () => !IsBusy);
        _resetCommand = new RelayCommand(ResetForm, () => !IsBusy);
        _clearFilterCommand = new RelayCommand(ClearFilters, () => !IsBusy);
    }

    public ObservableCollection<AdminStatTileViewModel> Stats { get; } = [];
    public ObservableCollection<AdminCargoRowViewModel> Cargo { get; } = [];
    public ObservableCollection<AdminChoiceViewModel> CargoTypeOptions { get; } =
    [
        new("normal", "Обычный"),
        new("hazardous", "Опасный"),
        new("perishable", "Скоропортящийся"),
        new("oversized", "Крупногабаритный")
    ];
    public ObservableCollection<AdminChoiceViewModel> CargoTypeFilterOptions { get; } =
    [
        new(string.Empty, "Все типы"),
        new("normal", "Обычный"),
        new("hazardous", "Опасный"),
        new("perishable", "Скоропортящийся"),
        new("oversized", "Крупногабаритный")
    ];
    public ObservableCollection<AdminChoiceViewModel> SortOptions { get; } =
    [
        new("-UpdatedAt", "Сначала обновлённые"),
        new("Name", "Наименование"),
        new("-WeightKg", "Самые тяжёлые"),
        new("-CreatedAt", "Сначала новые")
    ];

    public ICommand BeginCreateCommand => _beginCreateCommand;
    public ICommand SaveCommand => _saveCommand;
    public ICommand DeleteCommand => _deleteCommand;
    public ICommand ResetCommand => _resetCommand;
    public ICommand ApplyFilterCommand => _applyFilterCommand;
    public ICommand ClearFilterCommand => _clearFilterCommand;

    public AdminCargoRowViewModel? SelectedCargo
    {
        get => _selectedCargo;
        set => Set(ref _selectedCargo, value);
    }

    public uint? EditingCargoId
    {
        get => _editingCargoId;
        set => Set(ref _editingCargoId, value);
    }

    public string FilterSearch
    {
        get => _filterSearch;
        set => Set(ref _filterSearch, value);
    }

    public string FilterCargoTypeCode
    {
        get => _filterCargoTypeCode;
        set => Set(ref _filterCargoTypeCode, value);
    }

    public string SelectedSort
    {
        get => _selectedSort;
        set => Set(ref _selectedSort, value);
    }

    public string Name
    {
        get => _name;
        set => Set(ref _name, value);
    }

    public string WeightKg
    {
        get => _weightKg;
        set => Set(ref _weightKg, value);
    }

    public string VolumeM3
    {
        get => _volumeM3;
        set => Set(ref _volumeM3, value);
    }

    public string SelectedCargoTypeCode
    {
        get => _selectedCargoTypeCode;
        set => Set(ref _selectedCargoTypeCode, value);
    }

    public string Description
    {
        get => _description;
        set => Set(ref _description, value);
    }

    public string SpecialRequirements
    {
        get => _specialRequirements;
        set => Set(ref _specialRequirements, value);
    }

    public string FormTitle => EditingCargoId.HasValue ? "Редактирование груза" : "Новый груз";

    public override Task LoadLookupsAsync(CancellationToken cancellationToken = default)
    {
        if (!CargoTypeOptions.Any(x => x.Code == SelectedCargoTypeCode))
        {
            SelectedCargoTypeCode = "normal";
        }

        if (!CargoTypeFilterOptions.Any(x => x.Code == FilterCargoTypeCode))
        {
            FilterCargoTypeCode = string.Empty;
        }

        return Task.CompletedTask;
    }

    public void ApplyData(AdminCargoData data)
    {
        AdminCollectionHelper.ReplaceWith(
            Stats,
            data.Stats.Select(x => new AdminStatTileViewModel(x.Title, x.Value, x.Hint)));

        AdminCollectionHelper.ReplaceWith(
            Cargo,
            data.Cargo.Select(x => new AdminCargoRowViewModel
            {
                Id = x.Id,
                Name = x.Name,
                CargoType = x.CargoType,
                Weight = x.Weight,
                Volume = x.Volume,
                UsageCount = x.UsageCount,
                UpdatedAt = x.UpdatedAt,
                Description = x.Description,
                SpecialRequirements = x.SpecialRequirements
            }));
    }

    public override SieveModel BuildSieveModel()
    {
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(FilterSearch))
        {
            string value = AdminSieveHelper.NormalizeValue(FilterSearch);
            filters.Add($"(Name|CargoType)@={value}");
        }

        if (!string.IsNullOrWhiteSpace(FilterCargoTypeCode))
        {
            filters.Add($"CargoType=={AdminSieveHelper.NormalizeValue(FilterCargoTypeCode)}");
        }

        return new SieveModel
        {
            Filters = AdminSieveHelper.JoinFilters(filters),
            Sorts = AdminSieveHelper.NormalizeSort(SelectedSort)
        };
    }

    protected override void OnPropertyChanged(string? propertyName)
    {
        base.OnPropertyChanged(propertyName);

        switch (propertyName)
        {
            case nameof(SelectedCargo):
                _deleteArmedCargoId = null;
                _ = LoadSelectedCargoAsync(SelectedCargo);
                RaiseCommandStates();
                break;
            case nameof(EditingCargoId):
                OnPropertyChanged(nameof(FormTitle));
                break;
            case nameof(Name):
            case nameof(WeightKg):
            case nameof(VolumeM3):
            case nameof(SelectedCargoTypeCode):
                _saveCommand.RaiseCanExecuteChanged();
                break;
        }
    }

    protected override void RaiseCommandStates()
    {
        _beginCreateCommand.RaiseCanExecuteChanged();
        _saveCommand.RaiseCanExecuteChanged();
        _deleteCommand.RaiseCanExecuteChanged();
        _applyFilterCommand.RaiseCanExecuteChanged();
        _resetCommand.RaiseCanExecuteChanged();
        _clearFilterCommand.RaiseCanExecuteChanged();
    }

    private void ClearFilters()
    {
        FilterSearch = string.Empty;
        FilterCargoTypeCode = string.Empty;
        SelectedSort = "-UpdatedAt";
        _ = RefreshPanelAsync();
    }

    private void BeginCreate()
    {
        SelectedCargo = null;
        ResetForm();
        StatusMessage = "Заполните параметры нового груза и нажмите Сохранить.";
    }

    private void ResetForm()
    {
        EditingCargoId = null;
        Name = string.Empty;
        WeightKg = string.Empty;
        VolumeM3 = string.Empty;
        SelectedCargoTypeCode = "normal";
        Description = string.Empty;
        SpecialRequirements = string.Empty;
        _deleteArmedCargoId = null;
    }

    private async Task LoadSelectedCargoAsync(AdminCargoRowViewModel? cargo)
    {
        if (cargo is null)
        {
            ResetForm();
            return;
        }

        IsBusy = true;

        try
        {
            AdminCargoEditData? data = await AdminCrudService.GetCargoEditDataAsync(cargo.Id);
            if (data is null)
            {
                StatusMessage = "Груз не найден.";
                return;
            }

            EditingCargoId = data.Id;
            Name = data.Name;
            WeightKg = AdminParsingHelper.FormatDecimal(data.WeightKg);
            VolumeM3 = AdminParsingHelper.FormatDecimal(data.VolumeM3);
            SelectedCargoTypeCode = data.CargoType;
            Description = data.Description ?? string.Empty;
            SpecialRequirements = data.SpecialRequirements ?? string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки груза: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSave() =>
        !IsBusy
        && !string.IsNullOrWhiteSpace(Name)
        && AdminParsingHelper.TryParseRequiredDecimal(WeightKg, out _, out _)
        && AdminParsingHelper.TryParseNullableDecimal(VolumeM3, out _, out _);

    private async Task SaveAsync()
    {
        if (!AdminParsingHelper.TryParseRequiredDecimal(WeightKg, out decimal weightKg, out string? weightError))
        {
            StatusMessage = weightError;
            return;
        }

        if (!AdminParsingHelper.TryParseNullableDecimal(VolumeM3, out decimal? volumeM3, out string? volumeError))
        {
            StatusMessage = volumeError;
            return;
        }

        var data = new AdminCargoEditData(
            EditingCargoId,
            Name,
            SelectedCargoTypeCode,
            weightKg,
            volumeM3,
            Description,
            SpecialRequirements);

        if (EditingCargoId.HasValue)
        {
            await ExecuteCrudAsync(() => AdminCrudService.UpdateCargoAsync(data), "Груз обновлён.");
            return;
        }

        if (await ExecuteCrudAsync(() => AdminCrudService.CreateCargoAsync(data), "Груз создан."))
        {
            ResetForm();
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedCargo is null)
        {
            return;
        }

        if (_deleteArmedCargoId != SelectedCargo.Id)
        {
            _deleteArmedCargoId = SelectedCargo.Id;
            StatusMessage = $"Подтверждение: нажмите Удалить ещё раз, чтобы удалить груз {SelectedCargo.Name}.";
            return;
        }

        uint cargoId = SelectedCargo.Id;
        if (await ExecuteCrudAsync(() => AdminCrudService.DeleteCargoAsync(cargoId), "Груз удалён."))
        {
            ResetForm();
        }
    }
}
