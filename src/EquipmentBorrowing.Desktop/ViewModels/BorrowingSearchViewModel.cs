using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class BorrowingSearchViewModel : ViewModelBase
{
    private readonly BorrowingHistoryService _history;
    private int _version;
    private bool _suppress;

    public ObservableCollection<HistoryRow> Rows { get; } = new();

    public string[] StatusOptions { get; } = { "All", "Active", "Returned" };
    public string[] SortOptions { get; } = { "Checkout date", "Expected return date" };
    public string[] DirectionOptions { get; } = { "Descending", "Ascending" };

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private int statusIndex;

    [ObservableProperty]
    private int sortIndex;

    [ObservableProperty]
    private int directionIndex;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isError;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string emptyMessage = string.Empty;

    [ObservableProperty]
    private string resultSummary = string.Empty;

    public BorrowingSearchViewModel(BorrowingHistoryService history)
    {
        _history = history;
    }

    partial void OnSearchTextChanged(string value) => Trigger(300);

    partial void OnStatusIndexChanged(int value) => Trigger(0);

    partial void OnSortIndexChanged(int value) => Trigger(0);

    partial void OnDirectionIndexChanged(int value) => Trigger(0);

    [RelayCommand]
    private Task SearchAsync() => RunAsync(0);

    [RelayCommand]
    private Task ClearAsync()
    {
        _suppress = true;
        try
        {
            SearchText = string.Empty;
            StatusIndex = 0;
            SortIndex = 0;
            DirectionIndex = 0;
        }
        finally
        {
            _suppress = false;
        }

        return RunAsync(0);
    }

    private void Trigger(int delayMs)
    {
        if (!_suppress)
            _ = RunAsync(delayMs);
    }

    private async Task RunAsync(int delayMs)
    {
        var version = ++_version;

        if (delayMs > 0)
        {
            await Task.Delay(delayMs);
            if (version != _version)
                return;
        }

        IsLoading = true;
        SetStatus(null, false);

        var criteria = new BorrowingSearchCriteria(
            SearchText,
            (BorrowingStatusFilter)StatusIndex,
            (BorrowingSortField)SortIndex,
            DirectionIndex == 0);

        var result = await _history.SearchAsync(criteria);

        if (version != _version)
            return;

        IsLoading = false;
        Rows.Clear();

        if (result.State != QueryState.Loaded)
        {
            EmptyMessage = string.Empty;
            ResultSummary = string.Empty;
            SetStatus(result.Message, true);
            return;
        }

        foreach (var borrowing in result.Items)
            Rows.Add(HistoryRow.From(borrowing));

        EmptyMessage = Rows.Count == 0 ? "No borrowings match your criteria." : string.Empty;
        ResultSummary = Rows.Count == 0 ? string.Empty : $"{Rows.Count} record(s) shown";
    }

    private void SetStatus(string? message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }
}
