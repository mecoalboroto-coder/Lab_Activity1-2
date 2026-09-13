using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class EquipmentViewModel : ViewModelBase
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly BorrowEquipmentService _borrowEquipmentService;

    public ObservableCollection<Equipment> EquipmentItems { get; } = new();
    public ObservableCollection<Student> Students { get; } = new();

    [ObservableProperty]
    private Equipment? selectedEquipment;

    [ObservableProperty]
    private Student? selectedStudent;

    [ObservableProperty]
    private DateTimeOffset? expectedReturnDate = DateTimeOffset.Now.AddDays(7);

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isError;

    public event EventHandler? BorrowingCompleted;

    public EquipmentViewModel(
        IEquipmentRepository equipmentRepository,
        IStudentRepository studentRepository,
        BorrowEquipmentService borrowEquipmentService)
    {
        _equipmentRepository = equipmentRepository;
        _studentRepository = studentRepository;
        _borrowEquipmentService = borrowEquipmentService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var equipment = await _equipmentRepository.GetAllAsync();
        EquipmentItems.Clear();
        foreach (var item in equipment)
            EquipmentItems.Add(item);

        if (Students.Count == 0)
        {
            var students = await _studentRepository.GetAllAsync();
            foreach (var student in students)
                Students.Add(student);
        }
    }

    [RelayCommand]
    private async Task BorrowAsync()
    {
        if (SelectedStudent is null)
        {
            SetStatus("Please select a student.", isError: true);
            return;
        }

        if (SelectedEquipment is null)
        {
            SetStatus("Please select a piece of equipment.", isError: true);
            return;
        }

        if (ExpectedReturnDate is null)
        {
            SetStatus("Please choose an expected return date.", isError: true);
            return;
        }

        var returnDate = DateOnly.FromDateTime(ExpectedReturnDate.Value.DateTime);
        if (returnDate < DateOnly.FromDateTime(DateTime.Now))
        {
            SetStatus("Expected return date cannot be in the past.", isError: true);
            return;
        }

        var result = await _borrowEquipmentService.BorrowAsync(SelectedStudent.Id, SelectedEquipment.Id, returnDate);

        if (result.Success)
        {
            SetStatus($"Borrowed '{SelectedEquipment.Name}' for {SelectedStudent.Name}.", isError: false);
            SelectedEquipment = null;
            await LoadAsync();
            BorrowingCompleted?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            SetStatus(result.FailureReason ?? "The borrowing request could not be completed.", isError: true);
        }
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }
}
