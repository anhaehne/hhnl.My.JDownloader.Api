using hhnl.My.JDownloader.Api.Models.My;

namespace hhnl.My.JDownloader.Api.Utils;

public class MyJDownloaderSession
{
    private readonly string _regainToken;
    private readonly string _token;
    private readonly MyJDownloaderServerKey _initialServerKey;
    private readonly MyJDownloaderServerKey _serverKey;
    private readonly MyJDownloaderDeviceKey _initialDeviceKey;
    private readonly MyJDownloaderDeviceKey _deviceKey;
    private bool _isInvalidated;

    private MyJDownloaderSession(
        MyJDownloaderServerKey initialServerKey,
        MyJDownloaderServerKey serverKey,
        MyJDownloaderDeviceKey initialDeviceKey,
        MyJDownloaderDeviceKey deviceKey,
        string email,
        string token,
        string regainToken)
    {
        _initialServerKey = initialServerKey;
        _serverKey = serverKey;
        _initialDeviceKey = initialDeviceKey;
        _deviceKey = deviceKey;
        Email = email;
        _token = token;
        _regainToken = regainToken;
    }

    public MyJDownloaderServerKey InitialServerKey
    {
        get
        {
            return _isInvalidated
                ? throw new ObjectDisposedException("This session has been invalidated. Please create a new session.")
                : _initialServerKey;
        }
    }

    public MyJDownloaderServerKey ServerKey
    {
        get
        {
            return _isInvalidated
                ? throw new ObjectDisposedException("This session has been invalidated. Please create a new session.")
                : _serverKey;
        }
    }

    public MyJDownloaderDeviceKey InitialDeviceKey
    {
        get
        {
            return _isInvalidated
                ? throw new ObjectDisposedException("This session has been invalidated. Please create a new session.")
                : _initialDeviceKey;
        }
    }

    public MyJDownloaderDeviceKey DeviceKey
    {
        get
        {
            return _isInvalidated
                ? throw new ObjectDisposedException("This session has been invalidated. Please create a new session.")
                : _deviceKey;
        }
    }

    public string Token
    {
        get
        {
            return _isInvalidated
                ? throw new ObjectDisposedException("This session has been invalidated. Please create a new session.")
                : _token;
        }
    }

    public string RegainToken
    {
        get
        {
            return _isInvalidated
                ? throw new ObjectDisposedException("This session has been invalidated. Please create a new session.")
                : _regainToken;
        }
    }

    public string Email { get; }

    public static MyJDownloaderSession Create(MyJDownloaderServerKey initialServerKey, MyJDownloaderDeviceKey initialDeviceKey, LoginResponse loginResponse, string email)
        => new(
            initialServerKey,
            initialServerKey.CreateDerivedKey(loginResponse.Token),
            initialDeviceKey,
            initialDeviceKey.CreateDerivedKey(loginResponse.Token),
            email,
            loginResponse.Token, 
            loginResponse.RegainToken);

    public MyJDownloaderSession CreateRenewed(LoginResponse loginResponse)
    {
        var newSession = new MyJDownloaderSession(InitialServerKey, ServerKey.CreateDerivedKey(loginResponse.Token), InitialDeviceKey, DeviceKey.CreateDerivedKey(loginResponse.Token), Email, loginResponse.Token, loginResponse.RegainToken);
        ServerKey.Dispose();
        DeviceKey.Dispose();
        _isInvalidated = true;
        return newSession;
    }
}
