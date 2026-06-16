using System.Text.Json.Serialization;

namespace hhnl.My.JDownloader.Api.Models.My;

public class LoginResponse : MyJDownloaderApiResponseBase
{
    [JsonPropertyName("sessiontoken")]
    public required string Token { get; set; }

    [JsonPropertyName("regaintoken")]
    public required string RegainToken { get; set; }
}
