using System.Text.Json.Serialization;

namespace Requests.Application.Shelves;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class AddShelfRequest
{
    // Strict: the web defaults would otherwise accept numbers sent as strings, like "5".
    [JsonNumberHandling(JsonNumberHandling.Strict)]
    public int? ProductTypeCode { get; set; }

    [JsonNumberHandling(JsonNumberHandling.Strict)]
    public int? Capacity { get; set; }

    [JsonNumberHandling(JsonNumberHandling.Strict)]
    public double? Price { get; set; }
}
