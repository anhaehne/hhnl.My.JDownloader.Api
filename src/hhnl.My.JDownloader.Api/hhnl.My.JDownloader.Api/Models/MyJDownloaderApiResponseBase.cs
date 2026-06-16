using System.Text.Json.Serialization;

namespace hhnl.My.JDownloader.Api.Models;

public class MyJDownloaderApiResponseBase
{
    [JsonPropertyName("rid")]
    public long RequestId { get; set; }
}

public class MyJDownloaderApiResponse<TData> : MyJDownloaderApiResponseBase
{
    [JsonPropertyName("data")]
    public TData? Data { get; set; }

}
