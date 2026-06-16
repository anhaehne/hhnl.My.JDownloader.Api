using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Endpoints.Captcha;

[TestClass]
public sealed class CaptchaForwardEndpointTests : JDownloaderIntegrationTestBase
{
    // This endpoint creates and polls forwarded captcha jobs; the clean container has no stable read-only call to assert.
}
