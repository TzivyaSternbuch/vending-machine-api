namespace Requests.Api.Errors;

public class ErrorBody
{
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public List<ErrorDetail> Details { get; set; } = [];
}
