using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;
using hhnl.My.JDownloader.Api.Models;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Endpoints.Extensions;

[TestClass]
public sealed class ExtensionsEndpointTests : JDownloaderIntegrationTestBase
{
    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task ListAsync_should_returnExtensions()
    {
        var device = await CreateDeviceClientAsync();

        var extensions = await device.Extensions.ListAsync(new MyJdExtensionQuery { Name = true, Installed = true }, TestContext.CancellationToken);

        Assert.IsNotNull(extensions);
    }
}
