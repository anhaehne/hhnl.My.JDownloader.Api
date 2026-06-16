namespace hhnl.My.JDownloader.Api;

public class MyJDownloaderApiOptions
{
    public string ApplicationKey { get; set; } = "hhnl.My.JDownloader.Api";

    public string MyJDownloaderApiUrl { get; set; } = "https://api.jdownloader.org";

    public List<MyJDownloaderApiDeviceOptions> Devices { get; } = [];

    public bool DisableRemoteConnections { get; set; } = false;

    public bool DisableDirectConnections { get; set; } = false;

    public HttpsCertificateMode HttpsCertificateMode { get; set; }
}

public class MyJDownloaderApiDeviceOptions
{
    public required string Email { get; set; }

    public required string Password { get; set; }

    public required string DeviceName { get; set; }

    public string? CustomDirectConnectionEndpoint { get; set; }
}

public enum HttpsCertificateMode
{
    /// <summary>
    /// Default behavior. Verifies the certificate using the default .NET behavior.
    /// </summary>
    Verify,

    /// <summary>
    /// Trusts the certificate that is normally used when connecting directly to jDownloader.
    /// This is the recommended way if you are setting a <see cref="MyJDownloaderApiOptions.CustomDirectConnectionEndpoint"/>.
    /// </summary>
    TrustMyJDownloaderApiCertificate,

    /// <summary>
    /// Trust all certificates. Not recommended.
    /// </summary>
    TrustAll
}