using Requests.Domain.Machines;

namespace Requests.Application.Machines;

public class CreateMachineRequest
{
    public string? Name { get; set; }

    public LocationRequest? Location { get; set; }

    public int MaxShelves { get; set; } = VendingMachine.DEFAULT_MAX_SHELVES;
}
