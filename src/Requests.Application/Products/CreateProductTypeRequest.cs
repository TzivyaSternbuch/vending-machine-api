using System.Text.Json.Serialization;

namespace Requests.Application.Products;

/// <summary>
/// Unknown fields, including code, are rejected: the code is assigned by the server. categoryCode must be a
/// JSON number: the web defaults would otherwise accept "2".
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class CreateProductTypeRequest
{
    public string? Name { get; set; }

    [JsonNumberHandling(JsonNumberHandling.Strict)]
    public int? CategoryCode { get; set; }
}
