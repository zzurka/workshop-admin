using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using WorkshopAdmin.SharedKernel.Validation;
using Xunit;

namespace WorkshopAdmin.UnitTests.SharedKernel;

public class ValidationFilterTests
{
    private sealed record TestRequest(string Name);

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(r => r.Name).NotEmpty();
        }
    }

    private static async Task<object?> RunFilterAsync(TestRequest request, bool registerValidator = true)
    {
        ServiceCollection services = new();
        if (registerValidator)
        {
            services.AddScoped<IValidator<TestRequest>, TestRequestValidator>();
        }

        DefaultHttpContext httpContext = new()
        {
            RequestServices = services.BuildServiceProvider()
        };

        DefaultEndpointFilterInvocationContext context = new(httpContext, request);
        ValidationFilter<TestRequest> filter = new();

        return await filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>("handler-ran"));
    }

    [Fact]
    public async Task InvalidRequest_ShortCircuitsWith400()
    {
        object? result = await RunFilterAsync(new TestRequest(""));

        ValidationProblem problem = Assert.IsType<ValidationProblem>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Contains("Name", problem.ProblemDetails.Errors.Keys);
    }

    [Fact]
    public async Task ValidRequest_CallsHandler()
    {
        object? result = await RunFilterAsync(new TestRequest("ok"));

        Assert.Equal("handler-ran", result);
    }

    [Fact]
    public async Task NoValidatorRegistered_CallsHandler()
    {
        object? result = await RunFilterAsync(new TestRequest(""), registerValidator: false);

        Assert.Equal("handler-ran", result);
    }
}
