using hhnl.My.JDownloader.Api.Models;
using hhnl.My.JDownloader.Api.Utils;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Web;

namespace hhnl.My.JDownloader.Api;

public class MyJDownloaderServerHttpClient(IHttpClientFactory httpClientFactory)
{
    public async Task<T?> GetAsync<T>(string url, MyJDownloaderKey key, CancellationToken cancellationToken = default)
        where T : MyJDownloaderApiResponseBase
    {
        var result = await RequestAsync(url, key, null, cancellationToken);
        using var response = result.response;

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<MyJDownloaderApiRequestError>(cancellationToken: cancellationToken)
                ?? throw new MyJDownloaderApiException("The response failed without any error.");

            throw new MyJDownloaderApiRequestException(error);
        }

        using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var decryptedStream = key.DecryptFromBase64AsStream(content);

        var responseObject = await JsonSerializer.DeserializeAsync<T>(decryptedStream, cancellationToken: cancellationToken)
            ?? throw new MyJDownloaderApiException("The response could not be deserialized.");

        if (responseObject.RequestId != result.RequestId)
            throw new MyJDownloaderApiException($"The 'RequestId' differs from the 'RequestId' from the queryRequest. Response RequestId: {responseObject.RequestId}, Expected RequestId: {result.RequestId}");

        return responseObject;
    }

    public async Task<(HttpResponseMessage response, long RequestId)> RequestAsync(string url, MyJDownloaderKey key, string? param = null, CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient(nameof(MyJDownloaderServerHttpClient));

        var uri = new Uri(url, UriKind.RelativeOrAbsolute);

        if (!uri.IsAbsoluteUri)
            uri = new Uri(httpClient.BaseAddress ?? throw new MyJDownloaderApiException($"{nameof(MyJDownloaderServerHttpClient)} Base address is not set."), uri);

        var urlBuilder = new UriBuilder(uri);
        var encryptedParam = param is not null ? key.Encrypt(param) : null;

        var query = urlBuilder.Query;
        var queryParams = HttpUtility.ParseQueryString(query);

        if (encryptedParam is not null)
            queryParams["params"] = encryptedParam;

        var requestId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        queryParams["rid"] = requestId.ToString();

        urlBuilder.Query = "?" + string.Join("&", queryParams.AllKeys.Select(k => $"{k}={Uri.EscapeDataString(queryParams[k] ?? string.Empty)}"));

        var signature = key.ComputeHash(urlBuilder.Path + urlBuilder.Query);
        urlBuilder.Query += $"&signature={WebUtility.UrlEncode(signature)}";

        using var request = new HttpRequestMessage(encryptedParam is null ? HttpMethod.Get : HttpMethod.Post, urlBuilder.Uri);

        if (encryptedParam is not null)
            request.Content = new StringContent(encryptedParam, Encoding.UTF8, "application/aesjson-jd");

        return (await httpClient.SendAsync(request, cancellationToken), requestId);
    }
}


