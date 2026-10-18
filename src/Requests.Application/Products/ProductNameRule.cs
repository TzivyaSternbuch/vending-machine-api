using Requests.Application.Exceptions;

namespace Requests.Application.Products;

public static class ProductNameRule
{
    public const int NAME_MAX_LENGTH = 32;

    public static string? Normalize(string? name) => name?.Trim().ToLowerInvariant();

    public static void Validate(string? name)
    {
        var normalized = Normalize(name);

        if (string.IsNullOrEmpty(normalized))
        {
            throw new ValidationException("name", "is required");
        }

        if (normalized.Length > NAME_MAX_LENGTH)
        {
            throw new ValidationException("name", $"must be at most {NAME_MAX_LENGTH} characters");
        }

        if (!normalized.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-'))
        {
            throw new ValidationException("name", "must contain only letters, digits and '-'");
        }
    }
}
