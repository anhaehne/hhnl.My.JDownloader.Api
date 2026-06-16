using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Endpoints.Events;

[TestClass]
public sealed class EventsEndpointTests : JDownloaderIntegrationTestBase
{
    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task ListpublisherAsync_should_returnPublishers()
    {
        var device = await CreateDeviceClientAsync();

        var publishers = await device.Events.ListpublisherAsync(TestContext.CancellationToken);

        Assert.IsNotNull(publishers);
    }
}
