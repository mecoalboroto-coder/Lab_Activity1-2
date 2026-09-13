using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class BorrowingsViewModel : ViewModelBase
{
    private readonly IBorrowingRepository _borrowingRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly ReturnEquipmentService _returnEquipmentService;

    public ObservableCollection<BorrowingDisplayItem> ActiveBorrowings { get; } = new();

    [ObservableProperty]
    private BorrowingDisplayItem? selectedBorrowing;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isError;

    public event EventHandler? ReturnCompleted;

    public BorrowingsViewModel(
        IBorrowingRepository borrowingRepository,
        IStudentRepository studentRepository,
        IEquipmentRepository equipmentRepository,
        ReturnEquipmentService returnEquipmentService)
    {
        _borrowingRepository = borrowingRepository;
        _studentRepository = studentRepository;
        _equipmentRepository = equipmentRepository;
        _returnEquipmentService = returnEquipmentService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var active = await _borrowingRepository.GetActiveAsync();

        ActiveBorrowings.Clear();
        foreach (var borrowing in active)
        {
            var student = await _studentRepository.GetByIdAsync(borrowing.StudentId);
            var equipment = await _equipmentRepository.GetByIdAsync(borrowing.EquipmentId);

            ActiveBorrowings.Add(new BorrowingDisplayItem
            {
                BorrowingId = borrowing.Id,
                StudentName = student?.Name ?? $"Student {borrowing.StudentId}",
                EquipmentName = equipment?.Name ?? $"Equipment {borrowing.EquipmentId}",
                DateBorrowed = borrowing.DateBorrowed,
                ExpectedReturnDate = borrowing.ExpectedReturnDate
            });
        }
    }

    [RelayCommand]
    private async Task ReturnAsync()
    {
        if (SelectedBorrowing is null)
        {
            SetStatus("Please select a borrowing to return.", isError: true);
            return;
        }

        var result = await _returnEquipmentService.ReturnAsync(SelectedBorrowing.BorrowingId);

        if (result.Success)
        {
            SetStatus($"Returned '{SelectedBorrowing.EquipmentName}'.", isError: false);
            SelectedBorrowing = null;
            await LoadAsync();
            ReturnCompleted?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            SetStatus(result.FailureReason ?? "The return request could not be completed.", isError: true);
        }
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }
}
