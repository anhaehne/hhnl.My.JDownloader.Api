using System.Text.Json.Serialization;

namespace hhnl.My.JDownloader.Api.Models;

public class MyJdDeviceInfo
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("status")]
    public required string Status { get; set; }
}
