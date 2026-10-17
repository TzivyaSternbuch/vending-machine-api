namespace Requests.Application.Exceptions;

/// <summary>Thrown when a valid request clashes with what is already stored, e.g. a name that is taken.</summary>
public class ConflictException(string message) : Exception(message)
{
}
