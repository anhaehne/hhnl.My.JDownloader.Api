using hhnl.My.JDownloader.Api.Models;
using hhnl.My.JDownloader.Api.Utils;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace hhnl.My.JDownloader.Api;

public class MyJDownloaderDeviceHttpClient(IHttpClientFactory httpClientFactory)
{
    public static JsonSerializerOptions JsonSerializerOptions { get; } = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    };

    public Task<T> RequestAsync<T>(string url, MyJdDeviceInfo device, MyJDownloaderSession session, object[]? parameters = null, CancellationToken cancellationToken = default)
        => RequestAsync<T>(url, device.Id, session, parameters, cancellationToken);

    public async Task<T> RequestAsync<T>(string url, string deviceId, MyJDownloaderSession session, object[]? parameters = null, CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient(nameof(MyJDownloaderDeviceHttpClient));

        var requestId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var uri = new Uri(url, UriKind.RelativeOrAbsolute);

        if (!uri.IsAbsoluteUri)
            uri = new Uri(httpClient.BaseAddress ?? throw new MyJDownloaderApiException($"{nameof(MyJDownloaderDeviceHttpClient)} Base address is not set."), uri);

        var urlBuilder = new UriBuilder(uri);
        var originalPathAndQuery = urlBuilder.Path + urlBuilder.Query;
        urlBuilder.Path = $"/t_{HttpUtility.UrlEncode(session.Token)}_{HttpUtility.UrlEncode(deviceId)}{urlBuilder.Path}";

        var deviceRequest = new DeviceRequest(originalPathAndQuery, parameters, requestId);
        using var request = new HttpRequestMessage(HttpMethod.Post, urlBuilder.Uri);

        request.Content = new EncryptedJsonStreamContent(deviceRequest, session.DeviceKey);

        var response = await httpClient.SendAsync(request, cancellationToken);

        using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var decryptedStream = session.DeviceKey.DecryptFromBase64AsStream(content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await JsonSerializer.DeserializeAsync<MyJDownloaderApiRequestError>(decryptedStream, cancellationToken: cancellationToken)
                ?? throw new MyJDownloaderApiException("The response failed without any error.");

            throw new MyJDownloaderApiRequestException(error);
        }

        using var textReader = new StreamReader(decryptedStream);

        MyJDownloaderApiResponse<T> responseObject;

        if (true)
        {
            using var decryptedStream1 = session.DeviceKey.DecryptFromBase64AsStream(content, leaveOpen: true);
            using var textReader1 = new StreamReader(decryptedStream1);
            var json = await textReader1.ReadToEndAsync();
            content.Position = 0;
        }

        try
        {
            responseObject = await JsonSerializer.DeserializeAsync<MyJDownloaderApiResponse<T>>(decryptedStream, cancellationToken: cancellationToken)
                ?? throw new MyJDownloaderApiException("The response could not be deserialized.");
        }
        catch (JsonException ex)
        {
            var responseText = "Unable to read response content again.";

            try
            {
                content.Position = 0;
                using var decryptedStream1 = session.DeviceKey.DecryptFromBase64AsStream(content);
                using var textReader1 = new StreamReader(decryptedStream1);
                responseText = await textReader1.ReadToEndAsync();
            }
            catch (Exception)
            {
                // Ignore
            }

            throw new MyJDownloaderApiException($"Unable to deserialize json to type {typeof(T)}. Json:\r\n{responseText}", ex);
        }


        if (responseObject.RequestId != requestId)
            throw new MyJDownloaderApiException($"The 'RequestId' differs from the 'RequestId' from the queryRequest. Response RequestId: {responseObject.RequestId}, Expected RequestId: {requestId}");

        if(responseObject.Data is JsonElement jsonElement && typeof(T) == typeof(object))
        {
            // Any value is requests. Try to convert the json element into a value
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => (T)(object)jsonElement.GetString()!,
                JsonValueKind.Number => (T)(object)jsonElement.GetDouble(),
                JsonValueKind.True => (T)(object)true,
                JsonValueKind.False => (T)(object)false,
                JsonValueKind.Null => default!,
                _ => throw new MyJDownloaderApiException($"The response contains a JSON element of kind '{jsonElement.ValueKind}' which cannot be converted to the type 'object'."),
            };
        }

        return responseObject.Data!;
    }

    private record DeviceRequest([property: JsonPropertyName("url")] string Url, [property: JsonPropertyName("params"), JsonConverter(typeof(NestedJsonConverter))] object[]? Parameters, [property: JsonPropertyName("rid")] long RequestId)
    {
        [JsonPropertyName("apiVer")]
#pragma warning disable CA1822 // Mark members as static
        public int ApiVersion => 1;
#pragma warning restore CA1822 // Mark members as static
    }

    private class EncryptedJsonStreamContent(object payload, MyJDownloaderKey key) : HttpContent
    {
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            // This stream will receive the final Base64 output. We leave it open.
            await using var base64Stream = new CryptoStream(stream, new ToBase64Transform(), CryptoStreamMode.Write, leaveOpen: true);

            // This stream will encrypt data and write it to the base64Stream.
            await using var cryptoStream = key.EncryptAsStream(base64Stream);

            // Serialize the payload as JSON into the cryptoStream.
            // When cryptoStream is disposed, it will flush the final encrypted block,
            // which in turn will allow base64Stream to flush the final Base64 characters.
            await JsonSerializer.SerializeAsync(cryptoStream, payload, options: JsonSerializerOptions);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    private class NestedJsonConverter : JsonConverter<object[]>
    {
        public override object[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, object[] value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartArray();

            foreach (var element in value)
            {
                var nestedJson = JsonSerializer.Serialize(element, options);
                writer.WriteStringValue(nestedJson);
            }

            writer.WriteEndArray();
        }
    }
}
