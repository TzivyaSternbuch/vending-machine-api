using System.Text.Json.Serialization;

namespace Requests.Application.Products;

/// <summary>Unknown fields, including code, are rejected: the code is assigned by the server.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class CreateProductCategoryRequest
{
    public string? Name { get; set; }
}
