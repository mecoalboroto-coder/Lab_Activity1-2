using EquipmentBorrowing.Application.Services;

namespace EquipmentBorrowing.Api;

internal static class ResultMapper
{
    public static IResult Failure(FailureKind kind, bool outcomeUnknown, string message) => kind switch
    {
        FailureKind.Invalid => ApiResults.Error(400, "validation_failed", message),
        FailureKind.NotFound => ApiResults.Error(404, "not_found", message),
        FailureKind.Conflict => ApiResults.Error(409, "conflict", message),
        _ => ApiResults.Error(500, outcomeUnknown ? "outcome_unknown" : "server_error", message)
    };

    public static IResult Failure<T>(OperationResult<T> result) =>
        Failure(result.Kind, result.OutcomeUnknown, result.Message);

    public static IResult Failure(BorrowResult result) =>
        Failure(result.Kind, result.OutcomeUnknown, result.FailureReason ?? "The request could not be completed.");

    public static IResult Failure(ReturnResult result) =>
        Failure(result.Kind, result.OutcomeUnknown, result.FailureReason ?? "The request could not be completed.");
}
