using System.Text.Json.Serialization;

namespace Requests.Application.Shelves;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class LoadInventoryRequest
{
    public string? ShelfId { get; set; }

    public int? ProductTypeCode { get; set; }

    public int? Quantity { get; set; }
}
