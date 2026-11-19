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

public partial class StudentsViewModel : ViewModelBase
{
    private readonly IStudentRepository _repository;
    private readonly StudentManagementService _service;
    private List<Student> _all = new();
    private bool _suppressFill;

    public ObservableCollection<Student> Students { get; } = new();

    [ObservableProperty]
    private Student? selectedStudent;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string studentNumber = string.Empty;

    [ObservableProperty]
    private string fullName = string.Empty;

    [ObservableProperty]
    private string contact = string.Empty;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isError;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string emptyMessage = string.Empty;

    public event EventHandler? StudentsChanged;

    public StudentsViewModel(IStudentRepository repository, StudentManagementService service)
    {
        _repository = repository;
        _service = service;
    }

    partial void OnSelectedStudentChanged(Student? value)
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
            var students = await _repository.GetAllAsync();
            _all = students.ToList();
            ApplyFilter();

            if (IsError)
                SetStatus(null, false);
        }
        catch (Exception)
        {
            SetStatus("Could not load students from the database. Check that PostgreSQL is running, then press Refresh.", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        var result = await _service.RegisterAsync(StudentNumber, FullName, Contact);
        await CompleteAsync(result);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedStudent is null)
        {
            SetStatus("Select a student from the list before saving changes.", true);
            return;
        }

        var result = await _service.UpdateAsync(SelectedStudent.Id, StudentNumber, FullName, Contact);
        await CompleteAsync(result);
    }

    [RelayCommand]
    private Task PermitAsync() => ChangeEligibilityAsync(true);

    [RelayCommand]
    private Task RestrictAsync() => ChangeEligibilityAsync(false);

    [RelayCommand]
    private void Clear()
    {
        SelectedStudent = null;
        StudentNumber = string.Empty;
        FullName = string.Empty;
        Contact = string.Empty;
        SetStatus(null, false);
    }

    private async Task ChangeEligibilityAsync(bool allowed)
    {
        if (SelectedStudent is null)
        {
            SetStatus("Select a student from the list first.", true);
            return;
        }

        var result = await _service.SetEligibilityAsync(SelectedStudent.Id, allowed);
        await CompleteAsync(result);
    }

    private async Task CompleteAsync(OperationResult<Student> result)
    {
        await LoadAsync();

        if (result.Success && result.Value is not null)
        {
            var match = Students.FirstOrDefault(s => s.Id == result.Value.Id);
            SelectedStudent = match;
            if (match is not null)
                FillForm(match);

            StudentsChanged?.Invoke(this, EventArgs.Empty);
        }

        SetStatus(result.Message, !result.Success);
    }

    private void ApplyFilter()
    {
        var selectedId = SelectedStudent?.Id;
        var term = SearchText.Trim();

        IEnumerable<Student> query = _all;
        if (term.Length > 0)
        {
            query = query.Where(s =>
                s.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || s.StudentNumber.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var list = query.ToList();

        _suppressFill = true;
        try
        {
            Students.Clear();
            foreach (var student in list)
                Students.Add(student);

            SelectedStudent = selectedId is null ? null : Students.FirstOrDefault(s => s.Id == selectedId);
        }
        finally
        {
            _suppressFill = false;
        }

        EmptyMessage = Students.Count > 0
            ? string.Empty
            : _all.Count == 0 ? "No students are registered yet." : "No students match your search.";
    }

    private void FillForm(Student student)
    {
        StudentNumber = student.StudentNumber;
        FullName = student.Name;
        Contact = student.Contact ?? string.Empty;
    }

    private void SetStatus(string? message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }
}
