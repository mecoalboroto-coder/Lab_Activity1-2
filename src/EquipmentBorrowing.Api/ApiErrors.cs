using Microsoft.AspNetCore.Diagnostics;

namespace EquipmentBorrowing.Api;

internal static class ApiErrors
{
    public static async Task WriteServerErrorAsync(HttpContext context)
    {
        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        if (error is not null)
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("EquipmentBorrowing.Api");
            logger.LogError(error, "Unhandled error while processing {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new ErrorBody(
            "server_error",
            "The server could not complete the request. Please try again later."));
    }

    public static async Task WriteStatusCodeAsync(HttpContext context)
    {
        var (code, message) = context.Response.StatusCode switch
        {
            StatusCodes.Status404NotFound => ("not_found", "The requested resource was not found."),
            StatusCodes.Status405MethodNotAllowed => ("method_not_allowed", "That HTTP method is not supported for this route."),
            StatusCodes.Status400BadRequest => ("invalid_request", "The request is not valid."),
            _ => ("error", "The request could not be completed.")
        };

        await context.Response.WriteAsJsonAsync(new ErrorBody(code, message));
    }
}
