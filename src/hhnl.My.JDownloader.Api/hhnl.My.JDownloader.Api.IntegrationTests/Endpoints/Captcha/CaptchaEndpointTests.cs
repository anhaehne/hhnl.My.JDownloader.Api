using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Endpoints.Captcha;

[TestClass]
public sealed class CaptchaEndpointTests : JDownloaderIntegrationTestBase
{
    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task ListAsync_should_returnCaptchaJobs()
    {
        var device = await CreateDeviceClientAsync();

        var jobs = await device.Captcha.ListAsync(TestContext.CancellationToken);

        Assert.IsNotNull(jobs);
    }
}
