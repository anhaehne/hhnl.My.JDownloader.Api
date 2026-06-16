using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Endpoints.Dialogs;

[TestClass]
public sealed class DialogsEndpointTests : JDownloaderIntegrationTestBase
{
    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task ListAsync_should_returnDialogIds()
    {
        var device = await CreateDeviceClientAsync();

        var dialogIds = await device.Dialogs.ListAsync(TestContext.CancellationToken);

        Assert.IsNotNull(dialogIds);
    }
}
