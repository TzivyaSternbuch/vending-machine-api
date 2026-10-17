using Requests.Domain.Machines;

namespace Requests.Application.Machines;

public class MachineService(
    IMachineRepository repository,
    MachineValidator validator,
    TimeProvider timeProvider)
{
    public async Task<MachineResponse> CreateAsync(
        CreateMachineRequest request,
        string userId,
        CancellationToken cancellationToken)
    {
        validator.Validate(request);

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var machine = new VendingMachine
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name!.Trim(),
            Location = new Location
            {
                Region = request.Location!.Region!.Trim(),
                ZipCode = request.Location.ZipCode!,
                Description = request.Location.Description?.Trim(),
            },
            MaxShelves = request.MaxShelves,
            Shelves = [],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = userId,
            UpdatedBy = userId,
        };

        await repository.SaveAsync(machine, cancellationToken);

        return ToResponse(machine);
    }

    private static MachineResponse ToResponse(VendingMachine machine) => new()
    {
        Id = machine.Id,
        Name = machine.Name,
        Location = new LocationResponse
        {
            Region = machine.Location.Region,
            ZipCode = machine.Location.ZipCode,
            Description = machine.Location.Description,
        },
        MaxShelves = machine.MaxShelves,
        Shelves = machine.Shelves.Select(_ => new ShelfResponse()).ToList(),
        CreatedAt = machine.CreatedAt,
        UpdatedAt = machine.UpdatedAt,
        CreatedBy = machine.CreatedBy,
        UpdatedBy = machine.UpdatedBy,
    };
}
