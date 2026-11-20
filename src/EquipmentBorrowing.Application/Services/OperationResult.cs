namespace EquipmentBorrowing.Application.Services;

public sealed record OperationResult<T>(bool Success, string Message, T? Value, bool OutcomeUnknown)
{
    public static OperationResult<T> Ok(T value, string message) => new(true, message, value, false);

    public static OperationResult<T> Failure(string message) => new(false, message, default, false);

    public static OperationResult<T> Unknown(string message) => new(false, message, default, true);

    public static OperationResult<T> Validation(ArgumentException ex) =>
        Failure(ex.Message.Split('\n')[0].Split(" (Parameter")[0].Trim());
}
