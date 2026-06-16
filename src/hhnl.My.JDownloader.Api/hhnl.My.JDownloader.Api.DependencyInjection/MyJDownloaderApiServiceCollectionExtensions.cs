using hhnl.My.JDownloader.Api;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Collections.Frozen;

namespace Microsoft.Extensions.DependencyInjection;

public static class MyJDownloaderApiServiceCollectionExtensions
{
    public static IServiceCollection AddMyJDownloaderApi(this IServiceCollection services, Action<MyJDownloaderApiOptions>? configureOptions = null)
    {
        if (configureOptions is not null)
            services.Configure(configureOptions);

        var deviceClientConfig = services.AddHttpClient(nameof(MyJDownloaderDeviceHttpClient), (provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<MyJDownloaderApiOptions>>().Value;
                client.BaseAddress = new Uri(options.MyJDownloaderApiUrl);
            })
            .ConfigurePrimaryHttpMessageHandler(ConfigureHttpsCertificateMode)
            .AddHttpMessageHandler(provider => new MyJDownloaderDeviceHttpClientHandler(provider.GetRequiredService<IOptions<MyJDownloaderApiOptions>>()));

        services.AddHttpClient(nameof(MyJDownloaderServerHttpClient), (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<MyJDownloaderApiOptions>>().Value;
            client.BaseAddress = new Uri(options.MyJDownloaderApiUrl);
        });

        services.TryAddSingleton<MyJDownloaderServerClient>();
        services.TryAddSingleton<MyJDownloaderServerHttpClient>();
        services.TryAddSingleton<MyJDownloaderDeviceHttpClient>();
        return services;
    }

    public static IServiceCollection AddMyJDownloaderDevice(this IServiceCollection services, string email, string password, string name, string? customDirectConnectionEndpoint = null)
        => services.AddMyJDownloaderApi().Configure<MyJDownloaderApiOptions>(options => options.Devices.Add(new MyJDownloaderApiDeviceOptions
        {
            Email = email,
            Password = password,
            DeviceName = name,
            CustomDirectConnectionEndpoint = customDirectConnectionEndpoint
        }));

    private static HttpMessageHandler ConfigureHttpsCertificateMode(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<MyJDownloaderApiOptions>>().Value;

        if (options.HttpsCertificateMode == HttpsCertificateMode.TrustAll)
        {
            return new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (m, c, ch, e) => true
            };
        }

        return new HttpClientHandler();
    }
}

public class MyJDownloaderDeviceHttpClientHandler : DelegatingHandler
{
    private static readonly string _defaultMydnsJdownloaderUrl = "192-168-1-1.mydns.jdownloader.org";

    private readonly bool _appendHeader;
    private readonly FrozenSet<Uri> _deviceCustomEndpoints;

    public MyJDownloaderDeviceHttpClientHandler(IOptions<MyJDownloaderApiOptions> options)
    {
        _deviceCustomEndpoints = options.Value.Devices
            .Where(d => !string.IsNullOrWhiteSpace(d.CustomDirectConnectionEndpoint))
            .Select(x => x.CustomDirectConnectionEndpoint!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(d => new Uri(d))
            .ToFrozenSet();

        _appendHeader = _deviceCustomEndpoints.Count > 0;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // We force the Host header for HTTPS so we can validate the mydns.jdownloader.org certificate.
        if (_appendHeader
            && request.RequestUri is not null
            && _deviceCustomEndpoints.Any(e =>
                e.Scheme == Uri.UriSchemeHttps
                && request.RequestUri.Host.Equals(e.Host, StringComparison.OrdinalIgnoreCase)))
        {
            request.Headers.Remove("Host");
            request.Headers.Add("Host", _defaultMydnsJdownloaderUrl);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}