namespace hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;

[TestClass]
public static class JDownloaderIntegrationTestAssemblyLifecycle
{
    [AssemblyCleanup]
    public static Task AssemblyCleanup()
        => JDownloaderIntegrationTestBase.DisposeGlobalContainerAsync();
}
