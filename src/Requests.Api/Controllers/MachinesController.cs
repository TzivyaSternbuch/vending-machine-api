using Microsoft.AspNetCore.Mvc;
using Requests.Api.Authorization;
using Requests.Application.Machines;

namespace Requests.Api.Controllers;

[ApiController]
[AdminOnly]
[Route("machines")]
public class MachinesController(MachineService machineService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<MachineResponse>> Create(
        CreateMachineRequest request,
        [FromHeader(Name = AdminOnlyAttribute.USER_ID_HEADER)] string userId,
        CancellationToken cancellationToken)
    {
        var machine = await machineService.CreateAsync(request, userId, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, machine);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MachineResponse>> GetById(string id, CancellationToken cancellationToken)
    {
        var machine = await machineService.GetByIdAsync(id, cancellationToken);

        return Ok(machine);
    }
}
