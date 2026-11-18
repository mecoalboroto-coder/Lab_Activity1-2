using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public sealed record BorrowResult
{
    public bool Success { get; }
    public bool OutcomeUnknown { get; }
    public string? FailureReason { get; }
    public IReadOnlyList<Borrowing> Borrowings { get; }
    public Borrowing? Borrowing => Borrowings.FirstOrDefault();

    private BorrowResult(bool success, bool outcomeUnknown, string? failureReason, IReadOnlyList<Borrowing> borrowings)
    {
        Success = success;
        OutcomeUnknown = outcomeUnknown;
        FailureReason = failureReason;
        Borrowings = borrowings;
    }

    public static BorrowResult Succeeded(IReadOnlyList<Borrowing> borrowings) =>
        new(true, false, null, borrowings);

    public static BorrowResult Failed(string reason) =>
        new(false, false, reason, Array.Empty<Borrowing>());

    public static BorrowResult Unknown(string reason) =>
        new(false, true, reason, Array.Empty<Borrowing>());
}