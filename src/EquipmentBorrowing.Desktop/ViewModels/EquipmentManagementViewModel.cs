using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class EquipmentManagementViewModel : ViewModelBase
{
    private readonly IEquipmentRepository _repository;
    private readonly EquipmentManagementService _service;
    private List<Equipment> _all = new();
    private bool _suppressFill;

    public ObservableCollection<Equipment> Items { get; } = new();

    [ObservableProperty]
    private Equipment? selectedItem;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string identifier = string.Empty;

    [ObservableProperty]
    private string itemName = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isError;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string emptyMessage = string.Empty;

    public event EventHandler? EquipmentChanged;

    public EquipmentManagementViewModel(IEquipmentRepository repository, EquipmentManagementService service)
    {
        _repository = repository;
        _service = service;
    }

    partial void OnSelectedItemChanged(Equipment? value)
    {
        if (!_suppressFill && value is not null)
            FillForm(value);
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;

        try
        {
            var items = await _repository.GetAllAsync();
            _all = items.ToList();
            ApplyFilter();

            if (IsError)
                SetStatus(null, false);
        }
        catch (Exception)
        {
            SetStatus("Could not load equipment from the database. Check that PostgreSQL is running, then press Refresh.", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        var result = await _service.RegisterAsync(Identifier, ItemName, Description);
        await CompleteAsync(result);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedItem is null)
        {
            SetStatus("Select an item from the list before saving changes.", true);
            return;
        }

        var result = await _service.UpdateAsync(SelectedItem.Id, Identifier, ItemName, Description);
        await CompleteAsync(result);
    }

    [RelayCommand]
    private void Clear()
    {
        SelectedItem = null;
        Identifier = string.Empty;
        ItemName = string.Empty;
        Description = string.Empty;
        SetStatus(null, false);
    }

    private async Task CompleteAsync(OperationResult<Equipment> result)
    {
        await LoadAsync();

        if (result.Success && result.Value is not null)
        {
            var match = Items.FirstOrDefault(e => e.Id == result.Value.Id);
            SelectedItem = match;
            if (match is not null)
                FillForm(match);

            EquipmentChanged?.Invoke(this, EventArgs.Empty);
        }

        SetStatus(result.Message, !result.Success);
    }

    private void ApplyFilter()
    {
        var selectedId = SelectedItem?.Id;
        var term = SearchText.Trim();

        IEnumerable<Equipment> query = _all;
        if (term.Length > 0)
        {
            query = query.Where(e =>
                e.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || e.Identifier.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var list = query.ToList();

        _suppressFill = true;
        try
        {
            Items.Clear();
            foreach (var item in list)
                Items.Add(item);

            SelectedItem = selectedId is null ? null : Items.FirstOrDefault(e => e.Id == selectedId);
        }
        finally
        {
            _suppressFill = false;
        }

        EmptyMessage = Items.Count > 0
            ? string.Empty
            : _all.Count == 0 ? "No equipment is registered yet." : "No equipment matches your search.";
    }

    private void FillForm(Equipment item)
    {
        Identifier = item.Identifier;
        ItemName = item.Name;
        Description = item.Description ?? string.Empty;
    }

    private void SetStatus(string? message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }
}
