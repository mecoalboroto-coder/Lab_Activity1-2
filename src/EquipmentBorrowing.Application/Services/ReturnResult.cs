using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public sealed record ReturnResult
{
    public bool Success { get; }
    public string? FailureReason { get; }
    public Borrowing? Borrowing { get; }

    private ReturnResult(bool success, string? failureReason, Borrowing? borrowing)
    {
        Success = success;
        FailureReason = failureReason;
        Borrowing = borrowing;
    }

    public static ReturnResult Succeeded(Borrowing borrowing) => new(true, null, borrowing);

    public static ReturnResult Failed(string reason) => new(false, reason, null);
}