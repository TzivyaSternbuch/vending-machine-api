using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Requests.Application.Products;
using Requests.Domain.Users;

namespace Requests.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.ADMIN)]
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
