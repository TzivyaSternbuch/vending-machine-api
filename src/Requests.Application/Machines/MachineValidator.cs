using Requests.Application.Exceptions;

namespace Requests.Application.Machines;

/// <summary>
/// Validation rules for machine requests. Checks values as they will be stored (trimmed).
/// Throws <see cref="ValidationException"/> on the first broken rule.
/// </summary>
public class MachineValidator(MachineSettings settings)
{
    public const int NAME_MAX_LENGTH = 64;

    public const int REGION_MAX_LENGTH = 64;

    public const int DESCRIPTION_MAX_LENGTH = 256;

    public const int ZIP_CODE_MIN_LENGTH = 5;

    public const int ZIP_CODE_MAX_LENGTH = 10;

    public const int MIN_SHELVES = 1;

    public void Validate(CreateMachineRequest request)
    {
        ValidateName(request.Name);
        ValidateLocation(request.Location);
        ValidateMaxShelves(request.MaxShelves);
    }

    private static void ValidateName(string? name)
    {
        var trimmed = name?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new ValidationException("name", "is required");
        }

        if (trimmed.Length > NAME_MAX_LENGTH)
        {
            throw new ValidationException("name", $"must be at most {NAME_MAX_LENGTH} characters");
        }

        if (trimmed.Any(char.IsControl))
        {
            throw new ValidationException("name", "must not contain control characters");
        }
    }

    private static void ValidateLocation(LocationRequest? location)
    {
        if (location is null)
        {
            throw new ValidationException("location", "is required");
        }

        var region = location.Region?.Trim();

        if (string.IsNullOrEmpty(region))
        {
            throw new ValidationException("location.region", "is required");
        }

        if (region.Length > REGION_MAX_LENGTH)
        {
            throw new ValidationException("location.region", $"must be at most {REGION_MAX_LENGTH} characters");
        }

        if (location.ZipCode is null)
        {
            throw new ValidationException("location.zipCode", "is required");
        }

        if (location.ZipCode.Length < ZIP_CODE_MIN_LENGTH
            || location.ZipCode.Length > ZIP_CODE_MAX_LENGTH
            || !location.ZipCode.All(char.IsAsciiDigit))
        {
            throw new ValidationException("location.zipCode", $"must be {ZIP_CODE_MIN_LENGTH} to {ZIP_CODE_MAX_LENGTH} digits");
        }

        if (location.Description?.Trim().Length > DESCRIPTION_MAX_LENGTH)
        {
            throw new ValidationException("location.description", $"must be at most {DESCRIPTION_MAX_LENGTH} characters");
        }
    }

    private void ValidateMaxShelves(int maxShelves)
    {
        if (maxShelves < MIN_SHELVES || maxShelves > settings.MaxShelvesLimit)
        {
            throw new ValidationException("maxShelves", $"must be between {MIN_SHELVES} and {settings.MaxShelvesLimit}");
        }
    }
}
