using Microsoft.AspNetCore.Mvc;
using Requests.Api.Authorization;
using Requests.Application.Machines;
using Requests.Application.Shelves;

namespace Requests.Api.Controllers;

[ApiController]
[AdminOnly]
[Route("machines/{machineId}/shelves")]
public class ShelvesController(ShelfService shelfService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ShelfResponse>> Add(
        string machineId,
        AddShelfRequest request,
        [FromHeader(Name = AdminOnlyAttribute.USER_ID_HEADER)] string userId,
        CancellationToken cancellationToken)
    {
        var shelf = await shelfService.AddAsync(machineId, request, userId, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, shelf);
    }
}
