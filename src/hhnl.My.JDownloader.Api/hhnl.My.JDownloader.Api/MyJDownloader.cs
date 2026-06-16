using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace hhnl.My.JDownloader.Api;

public static class MyJDownloader
{
    public static Task<MyJDownloaderDevice> CreateDeviceClientAsync(string email, string password, string deviceName, CancellationToken cancellationToken = default)
        => CreateDeviceClientAsync(email, password, deviceName, configureOptions: null, cancellationToken);

    public static Task<MyJDownloaderDevice> CreateDeviceClientAsync(
        string email,
        string password,
        string deviceName,
        string? customDirectConnectionEndpoint,
        CancellationToken cancellationToken = default)
        => CreateDeviceClientAsync(email, password, deviceName, customDirectConnectionEndpoint, configureOptions: null, cancellationToken);

    public static Task<MyJDownloaderDevice> CreateDeviceClientAsync(
        string email,
        string password,
        string deviceName,
        Action<MyJDownloaderApiOptions>? configureOptions,
        CancellationToken cancellationToken = default)
        => CreateDeviceClientAsync(email, password, deviceName, customDirectConnectionEndpoint: null, configureOptions, cancellationToken);

    public static Task<MyJDownloaderDevice> CreateDeviceClientAsync(
        string email,
        string password,
        string deviceName,
        string? customDirectConnectionEndpoint,
        Action<MyJDownloaderApiOptions>? configureOptions,
        CancellationToken cancellationToken = default)
    {
        var options = CreateOptions(configureOptions);

        options.Devices.Add(new MyJDownloaderApiDeviceOptions
        {
            Email = email,
            Password = password,
            DeviceName = deviceName,
            CustomDirectConnectionEndpoint = customDirectConnectionEndpoint,
        });

        return CreateServerClient(options).CreateDeviceClientAsync(deviceName, cancellationToken);
    }

    public static Task<MyJDownloaderDevice> CreateDeviceClientAsync(MyJDownloaderApiDeviceOptions deviceOptions, CancellationToken cancellationToken = default)
        => CreateDeviceClientAsync(deviceOptions, configureOptions: null, cancellationToken);

    public static Task<MyJDownloaderDevice> CreateDeviceClientAsync(
        MyJDownloaderApiDeviceOptions deviceOptions,
        Action<MyJDownloaderApiOptions>? configureOptions,
        CancellationToken cancellationToken = default)
    {
        var options = CreateOptions(configureOptions);
        options.Devices.Add(deviceOptions);

        return CreateServerClient(options).CreateDeviceClientAsync(deviceOptions.DeviceName, cancellationToken);
    }

    public static MyJDownloaderServerClient CreateServerClient(Action<MyJDownloaderApiOptions>? configureOptions = null)
        => CreateServerClient(CreateOptions(configureOptions));

    private static MyJDownloaderApiOptions CreateOptions(Action<MyJDownloaderApiOptions>? configureOptions)
    {
        var options = new MyJDownloaderApiOptions();
        configureOptions?.Invoke(options);
        return options;
    }

    private static MyJDownloaderServerClient CreateServerClient(MyJDownloaderApiOptions options)
    {
        var optionsAccessor = Options.Create(options);
        var httpClientFactory = new MyJDownloaderHttpClientFactory(options);
        var serverHttpClient = new MyJDownloaderServerHttpClient(httpClientFactory);
        var deviceHttpClient = new MyJDownloaderDeviceHttpClient(httpClientFactory);

        return new MyJDownloaderServerClient(
            NullLogger<MyJDownloaderServerClient>.Instance,
            serverHttpClient,
            deviceHttpClient,
            optionsAccessor);
    }

    private sealed class MyJDownloaderHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _deviceHttpClient;
        private readonly HttpClient _serverHttpClient;

        public MyJDownloaderHttpClientFactory(MyJDownloaderApiOptions options)
        {
            _deviceHttpClient = CreateClient(options, nameof(MyJDownloaderDeviceHttpClient));
            _serverHttpClient = CreateClient(options, nameof(MyJDownloaderServerHttpClient));
        }

        public HttpClient CreateClient(string name)
            => name switch
            {
                nameof(MyJDownloaderDeviceHttpClient) => _deviceHttpClient,
                nameof(MyJDownloaderServerHttpClient) => _serverHttpClient,
                _ => throw new ArgumentException($"Unknown HTTP client name '{name}'.", nameof(name)),
            };

        private static HttpClient CreateClient(MyJDownloaderApiOptions options, string name)
            => new(CreateHttpMessageHandler(options, name))
            {
                BaseAddress = new Uri(options.MyJDownloaderApiUrl),
            };

        private static HttpMessageHandler CreateHttpMessageHandler(MyJDownloaderApiOptions options, string name)
        {
            var handler = new HttpClientHandler();

            if (options.HttpsCertificateMode == HttpsCertificateMode.TrustAll)
                handler.ServerCertificateCustomValidationCallback = (m, c, ch, e) => true;

            if (name != nameof(MyJDownloaderDeviceHttpClient))
                return handler;

            return new MyJDownloaderDeviceHttpClientHandler(options)
            {
                InnerHandler = handler,
            };
        }
    }

    private sealed class MyJDownloaderDeviceHttpClientHandler(MyJDownloaderApiOptions options) : DelegatingHandler
    {
        private const string DefaultMydnsJdownloaderUrl = "192-168-1-1.mydns.jdownloader.org";

        private readonly IReadOnlyCollection<Uri> _customDirectConnectionEndpoints = options.Devices
            .Where(d => !string.IsNullOrWhiteSpace(d.CustomDirectConnectionEndpoint))
            .Select(x => new Uri(x.CustomDirectConnectionEndpoint!))
            .ToArray();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Force the Host header for HTTPS so the mydns.jdownloader.org certificate remains valid for direct connections.
            if (request.RequestUri is not null
                && _customDirectConnectionEndpoints.Any(e =>
                    e.Scheme == Uri.UriSchemeHttps
                    && request.RequestUri.Host.Equals(e.Host, StringComparison.OrdinalIgnoreCase)))
            {
                request.Headers.Remove("Host");
                request.Headers.Add("Host", DefaultMydnsJdownloaderUrl);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
