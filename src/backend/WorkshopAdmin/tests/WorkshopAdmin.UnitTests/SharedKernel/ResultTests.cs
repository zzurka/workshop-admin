using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using WorkshopAdmin.SharedKernel.Results;
using Xunit;

namespace WorkshopAdmin.UnitTests.SharedKernel;

public class ResultTests
{
    [Fact]
    public void Success_HasNoError()
    {
        Result result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_CarriesError()
    {
        Error error = Error.NotFound("thing.not_found", "Thing not found.");

        Result result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void GenericSuccess_ExposesValue()
    {
        Result<int> result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_ThrowsOnValueAccess()
    {
        Result<int> result = Result.Failure<int>(Error.Failure("x", "boom"));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ImplicitConversions_Work()
    {
        Result<string> fromValue = "hello";
        Result<string> fromError = Error.Conflict("dup", "Duplicate.");

        Assert.True(fromValue.IsSuccess);
        Assert.False(fromError.IsSuccess);
    }

    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.Failure, StatusCodes.Status422UnprocessableEntity)]
    public void ToProblem_MapsErrorTypeToStatusCode(ErrorType errorType, int expectedStatus)
    {
        IResult httpResult = ResultHttpExtensions.ToProblem(new Error(errorType, "code", "message"));

        ProblemHttpResult problem = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal(expectedStatus, problem.StatusCode);
        Assert.Equal("code", problem.ProblemDetails.Extensions["code"]);
    }

    [Fact]
    public void ToHttpResult_Success_Returns204()
    {
        IResult httpResult = Result.Success().ToHttpResult();

        Assert.IsType<NoContent>(httpResult);
    }

    [Fact]
    public void ToHttpResult_GenericSuccess_Returns200WithValue()
    {
        IResult httpResult = Result.Success("payload").ToHttpResult();

        Ok<string> ok = Assert.IsType<Ok<string>>(httpResult);
        Assert.Equal("payload", ok.Value);
    }
}
