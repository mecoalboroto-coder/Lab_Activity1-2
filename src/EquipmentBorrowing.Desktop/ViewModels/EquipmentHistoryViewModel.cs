using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class EquipmentHistoryViewModel : ViewModelBase
{
    private readonly IEquipmentRepository _equipment;
    private readonly BorrowingHistoryService _history;
    private int _loadVersion;

    public ObservableCollection<Equipment> Items { get; } = new();
    public ObservableCollection<HistoryRow> Rows { get; } = new();

    [ObservableProperty]
    private Equipment? selectedItem;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isError;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string emptyMessage = "Select an item to view its borrowing history.";

    public EquipmentHistoryViewModel(IEquipmentRepository equipment, BorrowingHistoryService history)
    {
        _equipment = equipment;
        _history = history;
    }

    partial void OnSelectedItemChanged(Equipment? value) => _ = LoadHistoryAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var keptId = SelectedItem?.Id;

        try
        {
            var all = await _equipment.GetAllAsync();

            Items.Clear();
            foreach (var item in all)
                Items.Add(item);

            SelectedItem = keptId is null ? null : Items.FirstOrDefault(e => e.Id == keptId);

            if (keptId is not null && SelectedItem is null)
                SetStatus("The selected item no longer exists. Please select another item.", true);
        }
        catch (Exception)
        {
            SetStatus("Could not load equipment. Check that PostgreSQL is running, then press Refresh.", true);
        }
    }

    private async Task LoadHistoryAsync()
    {
        var version = ++_loadVersion;

        Rows.Clear();
        SetStatus(null, false);

        var item = SelectedItem;
        if (item is null)
        {
            IsLoading = false;
            EmptyMessage = "Select an item to view its borrowing history.";
            return;
        }

        IsLoading = true;
        EmptyMessage = string.Empty;

        var result = await _history.ForEquipmentAsync(item.Id);

        if (version != _loadVersion)
            return;

        IsLoading = false;

        if (result.State != QueryState.Loaded)
        {
            SetStatus(result.Message, true);
            return;
        }

        foreach (var borrowing in result.Items)
            Rows.Add(HistoryRow.From(borrowing));

        EmptyMessage = Rows.Count == 0 ? $"{item.Name} has no borrowing history." : string.Empty;
    }

    private void SetStatus(string? message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }
}
