using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Requests.Api.Errors;
using Requests.Domain.Errors;

namespace Requests.Tests.Api;

public class ResultExtensionsTests
{
    public static TheoryData<Error, int> Errors => new()
    {
        { Error.Validation("request.title_required", "Title is required."), StatusCodes.Status400BadRequest },
        { Error.NotFound("request.not_found", "Request 42 was not found."), StatusCodes.Status404NotFound },
        { Error.Conflict("request.already_closed", "Request 42 is already closed."), StatusCodes.Status409Conflict },
    };

    [Theory]
    [MemberData(nameof(Errors))]
    public void ToProblem_maps_type_to_status_and_keeps_code_and_message(Error error, int expectedStatus)
    {
        var problem = Assert.IsType<ProblemHttpResult>(error.ToProblem());

        Assert.Equal(expectedStatus, problem.StatusCode);
        Assert.Equal(error.Message, problem.ProblemDetails.Detail);
        Assert.Equal(error.Code, problem.ProblemDetails.Extensions["code"]);
    }

    [Fact]
    public void Every_error_type_has_a_mapping()
    {
        foreach (var type in Enum.GetValues<ErrorType>())
        {
            var problem = Assert.IsType<ProblemHttpResult>(new Error("test.code", "message", type).ToProblem());

            Assert.NotEqual(StatusCodes.Status500InternalServerError, problem.StatusCode);
        }
    }
}
