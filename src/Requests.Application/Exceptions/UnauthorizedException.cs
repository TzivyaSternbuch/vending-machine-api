namespace Requests.Application.Exceptions;

/// <summary>Thrown when credentials are wrong.</summary>
public class UnauthorizedException(string message) : Exception(message)
{
}
