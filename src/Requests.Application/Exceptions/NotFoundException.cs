namespace Requests.Application.Exceptions;

/// <summary>Thrown when a requested item does not exist.</summary>
public class NotFoundException(string message) : Exception(message)
{
}
