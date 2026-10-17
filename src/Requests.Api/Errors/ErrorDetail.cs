namespace Requests.Api.Errors;

/// <summary>One broken rule, e.g. field "location.zipCode", issue "must be 5 to 10 digits".</summary>
public class ErrorDetail
{
    public string Field { get; set; } = string.Empty;

    public string Issue { get; set; } = string.Empty;
}
