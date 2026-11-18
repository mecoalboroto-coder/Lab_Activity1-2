using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public sealed record ReturnResult
{
    public bool Success { get; }
    public bool OutcomeUnknown { get; }
    public string? FailureReason { get; }
    public Borrowing? Borrowing { get; }

    private ReturnResult(bool success, bool outcomeUnknown, string? failureReason, Borrowing? borrowing)
    {
        Success = success;
        OutcomeUnknown = outcomeUnknown;
        FailureReason = failureReason;
        Borrowing = borrowing;
    }

    public static ReturnResult Succeeded(Borrowing borrowing) => new(true, false, null, borrowing);

    public static ReturnResult Failed(string reason) => new(false, false, reason, null);

    public static ReturnResult Unknown(string reason) => new(false, true, reason, null);
}