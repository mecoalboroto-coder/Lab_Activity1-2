using System;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public sealed class HistoryRow
{
    public int BorrowingId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentNumber { get; init; } = string.Empty;
    public string EquipmentName { get; init; } = string.Empty;
    public string EquipmentIdentifier { get; init; } = string.Empty;
    public DateOnly DateBorrowed { get; init; }
    public DateOnly ExpectedReturnDate { get; init; }
    public DateOnly? ReturnedAt { get; init; }
    public string Status { get; init; } = string.Empty;

    public string ReturnedText => ReturnedAt is null
        ? "Not yet returned"
        : "Returned: " + ReturnedAt.Value.ToString("d");

    public static HistoryRow From(Borrowing b) => new()
    {
        BorrowingId = b.Id,
        StudentName = b.Student?.Name ?? $"Student {b.StudentId}",
        StudentNumber = b.Student?.StudentNumber ?? string.Empty,
        EquipmentName = b.Equipment?.Name ?? $"Equipment {b.EquipmentId}",
        EquipmentIdentifier = b.Equipment?.Identifier ?? string.Empty,
        DateBorrowed = b.DateBorrowed,
        ExpectedReturnDate = b.ExpectedReturnDate,
        ReturnedAt = b.ReturnedAt,
        Status = b.Status.ToString()
    };
}
