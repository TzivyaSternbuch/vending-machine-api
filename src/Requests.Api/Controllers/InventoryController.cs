using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Requests.Api.Authorization;
using Requests.Application.Machines;
using Requests.Application.Shelves;
using Requests.Domain.Users;

namespace Requests.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.ADMIN)]
[Route("machines/{machineId}/inventory")]
public class InventoryController(InventoryService inventoryService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ShelfResponse>> Load(
        string machineId,
        LoadInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var shelf = await inventoryService.LoadAsync(machineId, request, User.GetUserId(), cancellationToken);

        return Ok(shelf);
    }
}
