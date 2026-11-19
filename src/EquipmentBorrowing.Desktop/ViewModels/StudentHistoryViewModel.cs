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

public partial class StudentHistoryViewModel : ViewModelBase
{
    private readonly IStudentRepository _students;
    private readonly BorrowingHistoryService _history;
    private int _loadVersion;

    public ObservableCollection<Student> Students { get; } = new();
    public ObservableCollection<HistoryRow> Rows { get; } = new();

    [ObservableProperty]
    private Student? selectedStudent;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isError;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string emptyMessage = "Select a student to view their borrowing history.";

    public StudentHistoryViewModel(IStudentRepository students, BorrowingHistoryService history)
    {
        _students = students;
        _history = history;
    }

    partial void OnSelectedStudentChanged(Student? value) => _ = LoadHistoryAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var keptId = SelectedStudent?.Id;

        try
        {
            var all = await _students.GetAllAsync();

            Students.Clear();
            foreach (var student in all)
                Students.Add(student);

            SelectedStudent = keptId is null ? null : Students.FirstOrDefault(s => s.Id == keptId);

            if (keptId is not null && SelectedStudent is null)
                SetStatus("The selected student no longer exists. Please select another student.", true);
        }
        catch (Exception)
        {
            SetStatus("Could not load students. Check that PostgreSQL is running, then press Refresh.", true);
        }
    }

    private async Task LoadHistoryAsync()
    {
        var version = ++_loadVersion;

        Rows.Clear();
        SetStatus(null, false);

        var student = SelectedStudent;
        if (student is null)
        {
            IsLoading = false;
            EmptyMessage = "Select a student to view their borrowing history.";
            return;
        }

        IsLoading = true;
        EmptyMessage = string.Empty;

        var result = await _history.ForStudentAsync(student.Id);

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

        EmptyMessage = Rows.Count == 0 ? $"{student.Name} has no borrowing history." : string.Empty;
    }

    private void SetStatus(string? message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }
}
