using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly EquipmentViewModel _equipmentViewModel;
    private readonly BorrowingsViewModel _borrowingsViewModel;

    [ObservableProperty]
    private ViewModelBase currentViewModel;

    public MainWindowViewModel(EquipmentViewModel equipmentViewModel, BorrowingsViewModel borrowingsViewModel)
    {
        _equipmentViewModel = equipmentViewModel;
        _borrowingsViewModel = borrowingsViewModel;

        _equipmentViewModel.BorrowingCompleted += async (_, _) =>
            await _borrowingsViewModel.LoadCommand.ExecuteAsync(null);

        _borrowingsViewModel.ReturnCompleted += async (_, _) =>
            await _equipmentViewModel.LoadCommand.ExecuteAsync(null);

        currentViewModel = _equipmentViewModel;

        _ = _equipmentViewModel.LoadCommand.ExecuteAsync(null);
        _ = _borrowingsViewModel.LoadCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private void ShowEquipment() => CurrentViewModel = _equipmentViewModel;

    [RelayCommand]
    private void ShowBorrowings() => CurrentViewModel = _borrowingsViewModel;
}
