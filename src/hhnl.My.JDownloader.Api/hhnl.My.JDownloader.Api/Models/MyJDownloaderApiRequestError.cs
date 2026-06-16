using System.Text.Json.Serialization;

namespace hhnl.My.JDownloader.Api.Models;

public class MyJDownloaderApiRequestError
{
    [JsonPropertyName("src")]
    public required string Source { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }

    public override string ToString() => $"Source: {Source}, Type: {Type}";
}
