using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public sealed record ReturnResult
{
    public bool Success { get; }
    public bool OutcomeUnknown { get; }
    public FailureKind Kind { get; }
    public string? FailureReason { get; }
    public Borrowing? Borrowing { get; }

    private ReturnResult(bool success, bool outcomeUnknown, FailureKind kind, string? failureReason, Borrowing? borrowing)
    {
        Success = success;
        OutcomeUnknown = outcomeUnknown;
        Kind = kind;
        FailureReason = failureReason;
        Borrowing = borrowing;
    }

    public static ReturnResult Succeeded(Borrowing borrowing) => new(true, false, FailureKind.None, null, borrowing);

    public static ReturnResult Failed(string reason, FailureKind kind = FailureKind.Conflict) =>
        new(false, false, kind, reason, null);

    public static ReturnResult Unknown(string reason) => new(false, true, FailureKind.Server, reason, null);
}
