using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;
using hhnl.My.JDownloader.Api.Models;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Endpoints.Polling;

[TestClass]
public sealed class PollingEndpointTests : JDownloaderIntegrationTestBase
{
    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task PollAsync_should_returnPollingResults()
    {
        var device = await CreateDeviceClientAsync();

        var results = await device.Polling.PollAsync(new MyJdApiQuery(), TestContext.CancellationToken);

        Assert.IsNotNull(results);
    }
}
