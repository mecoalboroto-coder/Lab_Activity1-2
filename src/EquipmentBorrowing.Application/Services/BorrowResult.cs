using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

/// <summary>
/// Outcome of a Borrow Equipment attempt. Using a result record instead
/// of exceptions for expected business-rule failures keeps "student not
/// eligible" or "equipment unavailable" as ordinary control flow rather
/// than exceptional program state.
/// </summary>
public sealed record BorrowResult
{
    public bool Success { get; }
    public string? FailureReason { get; }
    public Borrowing? Borrowing { get; }

    private BorrowResult(bool success, string? failureReason, Borrowing? borrowing)
    {
        Success = success;
        FailureReason = failureReason;
        Borrowing = borrowing;
    }

    public static BorrowResult Succeeded(Borrowing borrowing) => new(true, null, borrowing);

    public static BorrowResult Failed(string reason) => new(false, reason, null);
}
