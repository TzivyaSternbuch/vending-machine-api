using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Requests.Api.Authorization;
using Requests.Application.Purchases;
using Requests.Domain.Users;

namespace Requests.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{Roles.USER},{Roles.ADMIN}")]
[Route("machines/{machineId}/shelves/{shelfId}/purchase")]
public class PurchasesController(PurchaseService purchaseService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PurchaseResponse>> Purchase(
        string machineId,
        string shelfId,
        CancellationToken cancellationToken)
    {
        var purchase = await purchaseService.PurchaseAsync(machineId, shelfId, User.GetUserId(), cancellationToken);

        return Ok(purchase);
    }
}
