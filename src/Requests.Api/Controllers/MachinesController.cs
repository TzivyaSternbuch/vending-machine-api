using Microsoft.AspNetCore.Mvc;
using Requests.Application.Machines;

namespace Requests.Api.Controllers;

[ApiController]
[Route("machines")]
public class MachinesController(MachineService machineService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<MachineResponse>> Create(
        CreateMachineRequest request,
        [FromHeader(Name = "X-User-Id")] string userId,
        CancellationToken cancellationToken)
    {
        var machine = await machineService.CreateAsync(request, userId, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, machine);
    }
}
