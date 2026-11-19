using System;
using CommunityToolkit.Mvvm.ComponentModel;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class SelectableEquipment : ObservableObject
{
    private readonly Action _changed;

    public Equipment Item { get; }

    public string Label => $"{Item.Identifier} - {Item.Name}";

    [ObservableProperty]
    private bool isSelected;

    public SelectableEquipment(Equipment item, Action changed)
    {
        Item = item;
        _changed = changed;
    }

    partial void OnIsSelectedChanged(bool value) => _changed();
}
