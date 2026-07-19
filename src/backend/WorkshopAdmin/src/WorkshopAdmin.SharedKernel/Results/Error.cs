namespace WorkshopAdmin.SharedKernel.Results;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Failure
}

/// <summary>
/// Expected failure of a use case. <paramref name="Code"/> is a stable machine-readable
/// key (e.g. <c>"work_order.not_found"</c>); <paramref name="Message"/> is for humans.
/// </summary>
public sealed record Error(ErrorType Type, string Code, string Message)
{
    public static Error Validation(string code, string message) => new(ErrorType.Validation, code, message);
    public static Error NotFound(string code, string message) => new(ErrorType.NotFound, code, message);
    public static Error Conflict(string code, string message) => new(ErrorType.Conflict, code, message);
    public static Error Unauthorized(string code, string message) => new(ErrorType.Unauthorized, code, message);
    public static Error Forbidden(string code, string message) => new(ErrorType.Forbidden, code, message);
    public static Error Failure(string code, string message) => new(ErrorType.Failure, code, message);
}
