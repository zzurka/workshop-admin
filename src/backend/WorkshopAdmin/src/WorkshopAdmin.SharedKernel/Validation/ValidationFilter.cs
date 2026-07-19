using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace WorkshopAdmin.SharedKernel.Validation;

/// <summary>
/// Endpoint filter that runs the registered <see cref="IValidator{T}"/> for the request
/// argument and short-circuits with 400 ProblemDetails when validation fails.
/// </summary>
public sealed class ValidationFilter<TRequest> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        IValidator<TRequest>? validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();
        if (validator is not null && context.Arguments.OfType<TRequest>().FirstOrDefault() is TRequest request)
        {
            ValidationResult validationResult =
                await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }
        }

        return await next(context);
    }
}

public static class ValidationFilterExtensions
{
    /// <summary>Attach request validation to an endpoint: <c>.WithValidation&lt;CreateThingRequest&gt;()</c>.</summary>
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter<ValidationFilter<TRequest>>();
}
