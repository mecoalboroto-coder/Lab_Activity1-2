using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly EquipmentViewModel _equipmentViewModel;
    private readonly BorrowingsViewModel _borrowingsViewModel;
    private readonly StudentsViewModel _studentsViewModel;
    private readonly EquipmentManagementViewModel _equipmentManagementViewModel;

    [ObservableProperty]
    private ViewModelBase currentViewModel;

    public MainWindowViewModel(
        EquipmentViewModel equipmentViewModel,
        BorrowingsViewModel borrowingsViewModel,
        StudentsViewModel studentsViewModel,
        EquipmentManagementViewModel equipmentManagementViewModel)
    {
        _equipmentViewModel = equipmentViewModel;
        _borrowingsViewModel = borrowingsViewModel;
        _studentsViewModel = studentsViewModel;
        _equipmentManagementViewModel = equipmentManagementViewModel;

        _equipmentViewModel.BorrowingCompleted += async (_, _) =>
        {
            await _borrowingsViewModel.LoadCommand.ExecuteAsync(null);
            await _equipmentManagementViewModel.LoadCommand.ExecuteAsync(null);
        };

        _borrowingsViewModel.ReturnCompleted += async (_, _) =>
        {
            await _equipmentViewModel.LoadCommand.ExecuteAsync(null);
            await _equipmentManagementViewModel.LoadCommand.ExecuteAsync(null);
        };

        _studentsViewModel.StudentsChanged += async (_, _) =>
            await _equipmentViewModel.LoadCommand.ExecuteAsync(null);

        _equipmentManagementViewModel.EquipmentChanged += async (_, _) =>
        {
            await _equipmentViewModel.LoadCommand.ExecuteAsync(null);
            await _borrowingsViewModel.LoadCommand.ExecuteAsync(null);
        };

        currentViewModel = _equipmentViewModel;

        _ = _equipmentViewModel.LoadCommand.ExecuteAsync(null);
        _ = _borrowingsViewModel.LoadCommand.ExecuteAsync(null);
        _ = _studentsViewModel.LoadCommand.ExecuteAsync(null);
        _ = _equipmentManagementViewModel.LoadCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private void ShowEquipment() => CurrentViewModel = _equipmentViewModel;

    [RelayCommand]
    private void ShowBorrowings() => CurrentViewModel = _borrowingsViewModel;

    [RelayCommand]
    private void ShowStudents() => CurrentViewModel = _studentsViewModel;

    [RelayCommand]
    private void ShowManageEquipment() => CurrentViewModel = _equipmentManagementViewModel;
}
