using Microsoft.AspNetCore.Http;

namespace WorkshopAdmin.SharedKernel.Results;

public static class ResultHttpExtensions
{
    /// <summary>204 on success, ProblemDetails on failure.</summary>
    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess ? TypedResults.NoContent() : ToProblem(result.Error!);

    /// <summary>200 with the value on success, ProblemDetails on failure.</summary>
    public static IResult ToHttpResult<TValue>(this Result<TValue> result) =>
        result.IsSuccess ? TypedResults.Ok(result.Value) : ToProblem(result.Error!);

    /// <summary>201 pointing at the created resource on success, ProblemDetails on failure.</summary>
    public static IResult ToCreatedResult<TValue>(this Result<TValue> result, Func<TValue, string> location) =>
        result.IsSuccess ? TypedResults.Created(location(result.Value), result.Value) : ToProblem(result.Error!);

    public static IResult ToProblem(Error error)
    {
        int statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status422UnprocessableEntity
        };

        return TypedResults.Problem(
            statusCode: statusCode,
            title: error.Type.ToString(),
            detail: error.Message,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
