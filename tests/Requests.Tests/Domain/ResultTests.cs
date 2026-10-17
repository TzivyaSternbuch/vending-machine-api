using Requests.Domain.Errors;

namespace Requests.Tests.Domain;

public class ResultTests
{
    [Fact]
    public void Failure_carries_code_and_type()
    {
        var result = Result.Failure(Error.Conflict("request.already_closed", "Request 42 is already closed."));

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("request.already_closed", result.Error.Code);
        Assert.Equal("Request 42 is already closed.", result.Error.Message);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public void Success_has_no_error()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Success_of_T_exposes_value()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Failure_of_T_has_no_value()
    {
        var result = Result.Failure<int>(Error.NotFound("request.not_found", "Request 42 was not found."));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Domain_does_not_reference_AspNetCore()
    {
        var references = typeof(Error).Assembly.GetReferencedAssemblies().Select(a => a.Name);

        Assert.DoesNotContain(references, name => name!.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }
}
