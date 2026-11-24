namespace EquipmentBorrowing.Application.Services;

public sealed record OperationResult<T>(bool Success, string Message, T? Value, bool OutcomeUnknown, FailureKind Kind)
{
    public static OperationResult<T> Ok(T value, string message) =>
        new(true, message, value, false, FailureKind.None);

    public static OperationResult<T> Failure(string message, FailureKind kind) =>
        new(false, message, default, false, kind);

    public static OperationResult<T> Unknown(string message) =>
        new(false, message, default, true, FailureKind.Server);

    public static OperationResult<T> Validation(ArgumentException ex) =>
        Failure(ex.Message.Split('\n')[0].Split(" (Parameter")[0].Trim(), FailureKind.Invalid);
}
