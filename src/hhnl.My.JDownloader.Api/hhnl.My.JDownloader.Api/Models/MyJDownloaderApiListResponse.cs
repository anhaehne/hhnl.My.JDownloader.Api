using System.Text.Json.Serialization;

namespace hhnl.My.JDownloader.Api.Models;

public class MyJDownloaderApiListResponse<T> : MyJDownloaderApiResponseBase
{
    [JsonPropertyName("list")]
    public required IReadOnlyCollection<MyJdDeviceInfo> List { get; set; }
}
