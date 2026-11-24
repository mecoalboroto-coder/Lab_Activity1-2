using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public sealed record BorrowResult
{
    public bool Success { get; }
    public bool OutcomeUnknown { get; }
    public FailureKind Kind { get; }
    public string? FailureReason { get; }
    public IReadOnlyList<Borrowing> Borrowings { get; }
    public Borrowing? Borrowing => Borrowings.FirstOrDefault();

    private BorrowResult(bool success, bool outcomeUnknown, FailureKind kind, string? failureReason, IReadOnlyList<Borrowing> borrowings)
    {
        Success = success;
        OutcomeUnknown = outcomeUnknown;
        Kind = kind;
        FailureReason = failureReason;
        Borrowings = borrowings;
    }

    public static BorrowResult Succeeded(IReadOnlyList<Borrowing> borrowings) =>
        new(true, false, FailureKind.None, null, borrowings);

    public static BorrowResult Failed(string reason, FailureKind kind = FailureKind.Conflict) =>
        new(false, false, kind, reason, Array.Empty<Borrowing>());

    public static BorrowResult Unknown(string reason) =>
        new(false, true, FailureKind.Server, reason, Array.Empty<Borrowing>());
}
