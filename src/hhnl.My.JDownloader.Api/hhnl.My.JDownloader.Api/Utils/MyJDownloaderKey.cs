using System.Security.Cryptography;
using System.Text;

namespace hhnl.My.JDownloader.Api.Utils;

public class MyJDownloaderServerKey : MyJDownloaderKey
{
    public MyJDownloaderServerKey(SymmetricAlgorithm encryptionAlgorithm, HMACSHA256 hMACSHA256, byte[] key)
        : base(encryptionAlgorithm, hMACSHA256, key, KeyDomain.Server)
    {
    }

    public MyJDownloaderServerKey CreateDerivedKey(string token) => CreateServerKey(CreateDerivedKeyBytes(token));
}

public class MyJDownloaderDeviceKey : MyJDownloaderKey
{
    public MyJDownloaderDeviceKey(SymmetricAlgorithm encryptionAlgorithm, HMACSHA256 hMACSHA256, byte[] key)
        : base(encryptionAlgorithm, hMACSHA256, key, KeyDomain.Device)
    {
    }

    public MyJDownloaderDeviceKey CreateDerivedKey(string token) => CreateDeviceKey(CreateDerivedKeyBytes(token));
}

public abstract class MyJDownloaderKey : IDisposable
{
    private readonly SymmetricAlgorithm _encryptionAlgorithm;
    private readonly HMACSHA256 _hMACSHA256;
    private readonly byte[] _key;
    private static readonly SHA256 _sHA256 = SHA256.Create();
    private bool _disposed;

    public KeyDomain Domain { get; }

    protected MyJDownloaderKey(SymmetricAlgorithm encryptionAlgorithm, HMACSHA256 hMACSHA256, byte[] key, KeyDomain domain)
    {
        _encryptionAlgorithm = encryptionAlgorithm;
        _hMACSHA256 = hMACSHA256;
        _key = key;
        Domain = domain;
    }

    public static MyJDownloaderServerKey CreateServerKey(string email, string password)
        => CreateServerKey(_sHA256.ComputeHash(Encoding.UTF8.GetBytes(email.ToLower() + password + "server")));

    public static MyJDownloaderDeviceKey CreateDeviceKey(string email, string password)
        => CreateDeviceKey(_sHA256.ComputeHash(Encoding.UTF8.GetBytes(email.ToLower() + password + "device")));

    public static MyJDownloaderServerKey CreateServerKey(byte[] key)
    {
        if (key.Length != 32)
            throw new ArgumentException("Key must be 32 bytes long.", nameof(key));

        // TODO: find out why this works but AES does not.
        // AES produces results where some data is preceding the actual data.

#pragma warning disable SYSLIB0022 // Type or member is obsolete
        var rj = new RijndaelManaged
        {
            BlockSize = 128,
            Mode = CipherMode.CBC,
            IV = key[..16].ToArray(),
            Key = key[16..].ToArray()
        };
#pragma warning restore SYSLIB0022 // Type or member is obsolete

        return new MyJDownloaderServerKey(rj, new HMACSHA256(key), key);
    }

    public static MyJDownloaderDeviceKey CreateDeviceKey(byte[] key)
    {
        if (key.Length != 32)
            throw new ArgumentException("Key must be 32 bytes long.", nameof(key));

        // TODO: find out why this works but AES does not.
        // AES produces results where some data is preceding the actual data.

#pragma warning disable SYSLIB0022 // Type or member is obsolete
        var rj = new RijndaelManaged
        {
            BlockSize = 128,
            Mode = CipherMode.CBC,
            IV = key[..16].ToArray(),
            Key = key[16..].ToArray()
        };
#pragma warning restore SYSLIB0022 // Type or member is obsolete

        return new MyJDownloaderDeviceKey(rj, new HMACSHA256(key), key);
    }

    public string Encrypt(string toEncrypt)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MyJDownloaderKey));

        using var ms = new MemoryStream();

        using (var cs = new CryptoStream(ms, _encryptionAlgorithm.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(toEncrypt);
        }

        if (ms.TryGetBuffer(out var buffer))
            return Convert.ToBase64String(buffer.AsSpan(..(int)ms.Position));

        return Convert.ToBase64String(ms.ToArray());
    }

    public Stream DecryptFromBase64AsStream(Stream stream, bool leaveOpen = false)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MyJDownloaderKey));

        return new CryptoStream(new CryptoStream(stream, new FromBase64Transform(), CryptoStreamMode.Read, leaveOpen), _encryptionAlgorithm.CreateDecryptor(), CryptoStreamMode.Read);
    }


    public Stream EncryptAsStream(Stream stream, bool leaveOpen = false)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MyJDownloaderKey));

        return new CryptoStream(stream, _encryptionAlgorithm.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen);
    }


    public string ComputeHash(string data)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MyJDownloaderKey));

        var dataBytes = Encoding.UTF8.GetBytes(data);
        var hash = _hMACSHA256.ComputeHash(dataBytes);
        return Convert.ToHexStringLower(hash);
    }

    protected byte[] CreateDerivedKeyBytes(string token)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MyJDownloaderKey));

        var bytes = Convert.FromHexString(token.Replace("-", ""));

        var buffer = new byte[bytes.Length + _key.Length];
        _key.CopyTo(buffer, 0);
        bytes.CopyTo(buffer, 32);
        return _sHA256.ComputeHash(buffer);
    }

    public void Dispose()
    {
        _encryptionAlgorithm.Dispose();
        _hMACSHA256.Dispose();
        _disposed = true;
    }

    public enum KeyDomain { Server, Device }
}
