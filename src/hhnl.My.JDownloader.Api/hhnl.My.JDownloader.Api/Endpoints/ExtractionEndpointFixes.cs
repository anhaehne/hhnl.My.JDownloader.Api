using hhnl.My.JDownloader.Api.Models;
using System.Text.Json;

namespace hhnl.My.JDownloader.Api.Endpoints;

public class ExtractionEndpointFixes : ExtractionEndpoint
{
    private readonly MyJDownloaderDevice _device;

    public ExtractionEndpointFixes(MyJDownloaderDevice device) : base(device)
    {
        _device = device;
    }

    public override async Task<bool> SetArchiveSettingsAsync(string archiveId, MyJdArchiveSettings archiveSettings, CancellationToken cancellationToken = default)
    {
        // These parameters have to be serialized individually, otherwise the API call fails.

        return await _device.RequestAsync<bool>(
                "/extraction/setArchiveSettings",
                [
                    JsonSerializer.Serialize(archiveId, MyJDownloaderDeviceHttpClient.JsonSerializerOptions),
                    JsonSerializer.Serialize(archiveSettings, MyJDownloaderDeviceHttpClient.JsonSerializerOptions),
                ],
                cancellationToken);
    }
}
