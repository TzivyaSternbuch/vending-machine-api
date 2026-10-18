namespace Requests.Application.Exceptions;

/// <summary>Thrown when a request breaks a validation rule.</summary>
public class ValidationException(string field, string issue) : Exception($"{field} {issue}")
{
    public string Field { get; } = field;

    public string Issue { get; } = issue;
}
