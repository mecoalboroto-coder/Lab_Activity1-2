using System;

namespace EquipmentBorrowing.Desktop.ViewModels;

public sealed class BorrowingDisplayItem
{
    public int BorrowingId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string EquipmentName { get; init; } = string.Empty;
    public DateOnly DateBorrowed { get; init; }
    public DateOnly ExpectedReturnDate { get; init; }
}
