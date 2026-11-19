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

public partial class MultiCheckoutViewModel : ViewModelBase
{
    private readonly IStudentRepository _students;
    private readonly IEquipmentRepository _equipment;
    private readonly MultiCheckoutService _service;

    public ObservableCollection<Student> Students { get; } = new();
    public ObservableCollection<SelectableEquipment> Items { get; } = new();

    [ObservableProperty]
    private Student? selectedStudent;

    [ObservableProperty]
    private DateTimeOffset? expectedReturnDate = DateTimeOffset.Now.AddDays(7);

    [ObservableProperty]
    private int selectedCount;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isError;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string emptyMessage = string.Empty;

    public event EventHandler? CheckoutCompleted;

    public MultiCheckoutViewModel(
        IStudentRepository students,
        IEquipmentRepository equipment,
        MultiCheckoutService service)
    {
        _students = students;
        _equipment = equipment;
        _service = service;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;

        var keptStudentId = SelectedStudent?.Id;
        var keptItemIds = Items.Where(i => i.IsSelected).Select(i => i.Item.Id).ToHashSet();

        try
        {
            var students = await _students.GetAllAsync();
            var available = await _equipment.GetAvailableAsync();

            Students.Clear();
            foreach (var student in students)
                Students.Add(student);

            SelectedStudent = keptStudentId is null ? null : Students.FirstOrDefault(s => s.Id == keptStudentId);

            Items.Clear();
            foreach (var item in available)
            {
                Items.Add(new SelectableEquipment(item, UpdateCount)
                {
                    IsSelected = keptItemIds.Contains(item.Id)
                });
            }

            UpdateCount();
            EmptyMessage = Items.Count == 0 ? "No equipment is currently available." : string.Empty;
        }
        catch (Exception)
        {
            SetStatus("Could not load data from the database. Check that PostgreSQL is running, then press Refresh.", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CheckoutAsync()
    {
        if (SelectedStudent is null)
        {
            SetStatus("Please select a student.", true);
            return;
        }

        if (ExpectedReturnDate is null)
        {
            SetStatus("Please choose an expected return date.", true);
            return;
        }

        var ids = Items.Where(i => i.IsSelected).Select(i => i.Item.Id).ToList();
        var date = DateOnly.FromDateTime(ExpectedReturnDate.Value.DateTime);
        var studentName = SelectedStudent.Name;

        var result = await _service.CheckoutAsync(SelectedStudent.Id, ids, date);

        await LoadAsync();

        if (result.Success)
        {
            SetStatus($"Checked out {result.Borrowings.Count} items for {studentName}.", false);
            CheckoutCompleted?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            SetStatus(result.FailureReason ?? "The checkout could not be completed.", true);
            if (result.OutcomeUnknown)
                CheckoutCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    private void UpdateCount() => SelectedCount = Items.Count(i => i.IsSelected);

    private void SetStatus(string? message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }
}
