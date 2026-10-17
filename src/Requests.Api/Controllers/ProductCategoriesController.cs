using Microsoft.AspNetCore.Mvc;
using Requests.Api.Authorization;
using Requests.Application.Products;

namespace Requests.Api.Controllers;

[ApiController]
[AdminOnly]
[Route("product-categories")]
public class ProductCategoriesController(ProductCategoryService categoryService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ProductCategoryResponse>> Create(
        CreateProductCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await categoryService.CreateAsync(request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, category);
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductCategoryResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var categories = await categoryService.GetAllAsync(cancellationToken);

        return Ok(categories);
    }
}
