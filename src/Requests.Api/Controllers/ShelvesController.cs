using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Requests.Api.Authorization;
using Requests.Application.Machines;
using Requests.Application.Shelves;
using Requests.Domain.Users;

namespace Requests.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.ADMIN)]
[Route("machines/{machineId}/shelves")]
public class ShelvesController(ShelfService shelfService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ShelfResponse>> Add(
        string machineId,
        AddShelfRequest request,
        CancellationToken cancellationToken)
    {
        var shelf = await shelfService.AddAsync(machineId, request, User.GetUserId(), cancellationToken);

        return StatusCode(StatusCodes.Status201Created, shelf);
    }
}
