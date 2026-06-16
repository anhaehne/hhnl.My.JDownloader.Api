using System.Text.Json.Serialization;

namespace hhnl.My.JDownloader.Api.Models;

public class MyJdDirectConnectionInfos
{
    [JsonPropertyName("infos")]
    public required IReadOnlyCollection<IpAndPort> Entries { get; set; }

    [JsonPropertyName("rebindProtectionDetected")]
    public bool RebindProtectionDetected { get; set; }

    [JsonPropertyName("mode")]
    public required string Mode { get; set; }
}

public class IpAndPort
{
    [JsonPropertyName("port")]
    public int Port { get; set; }

    [JsonPropertyName("ip")]
    public required string IpAddress { get; set; }
}