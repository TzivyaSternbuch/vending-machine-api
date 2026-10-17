using Microsoft.AspNetCore.Mvc;
using Requests.Api.Authorization;
using Requests.Application.Products;

namespace Requests.Api.Controllers;

[ApiController]
[AdminOnly]
[Route("product-types")]
public class ProductTypesController(ProductTypeService productTypeService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ProductTypeResponse>> Create(
        CreateProductTypeRequest request,
        CancellationToken cancellationToken)
    {
        var productType = await productTypeService.CreateAsync(request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, productType);
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductTypeResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var productTypes = await productTypeService.GetAllAsync(cancellationToken);

        return Ok(productTypes);
    }
}
