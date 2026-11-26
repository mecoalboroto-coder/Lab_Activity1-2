namespace EquipmentBorrowing.Api;

internal static class ApiResults
{
    public static IResult Error(int status, string code, string message) =>
        Results.Json(new ErrorBody(code, message), statusCode: status);

    public static IResult NotFound(string message) => Error(StatusCodes.Status404NotFound, "not_found", message);

    public static IResult ServerError() =>
        Error(
            StatusCodes.Status500InternalServerError,
            "server_error",
            "The server could not complete the request. Please try again later.");
}
