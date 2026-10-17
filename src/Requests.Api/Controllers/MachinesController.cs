using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Requests.Api.Authorization;
using Requests.Application.Machines;
using Requests.Domain.Users;

namespace Requests.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.ADMIN)]
[Route("machines")]
public class MachinesController(MachineService machineService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<MachineResponse>> Create(CreateMachineRequest request, CancellationToken cancellationToken)
    {
        var machine = await machineService.CreateAsync(request, User.GetUserId(), cancellationToken);

        return StatusCode(StatusCodes.Status201Created, machine);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MachineResponse>> GetById(string id, CancellationToken cancellationToken)
    {
        var machine = await machineService.GetByIdAsync(id, cancellationToken);

        return Ok(machine);
    }
}
