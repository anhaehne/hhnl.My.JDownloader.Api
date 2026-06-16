using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;

internal sealed class StaticFileContainer : IAsyncDisposable
{
    private const ushort HttpPort = 80;

    private readonly IContainer _container;
    private readonly string _tempDirectory;

    private StaticFileContainer(IContainer container, string tempDirectory, string networkAlias, string fileName)
    {
        _container = container;
        _tempDirectory = tempDirectory;
        ContainerUrl = $"http://{networkAlias}/{Uri.EscapeDataString(fileName)}";
    }

    public string ContainerUrl { get; }

    public static async Task<StaticFileContainer> StartAsync(INetwork network, string fileName, byte[] content, CancellationToken cancellationToken)
    {
        var networkAlias = $"static-file-{Guid.NewGuid():N}";
        var tempDirectory = Path.Combine(Path.GetTempPath(), "hhnl-my-jdownloader-api", Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(tempDirectory, fileName);

        Directory.CreateDirectory(tempDirectory);
        await File.WriteAllBytesAsync(filePath, content, cancellationToken);

        var container = new ContainerBuilder("nginx:alpine")
            .WithNetwork(network)
            .WithNetworkAliases(networkAlias)
            .WithResourceMapping(FilePath.Of(filePath), FilePath.Of($"/usr/share/nginx/html/{fileName}"))
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(HttpPort))
            .Build();

        await container.StartAsync(cancellationToken);

        return new StaticFileContainer(container, tempDirectory, networkAlias, fileName);
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();

        try
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
