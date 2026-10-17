using System.Net;
using System.Net.Http.Headers;
using Frontend.ViewModels;

namespace Frontend.Services;

public sealed class AuthorizedApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuthViewModel _authViewModel;

    public AuthorizedApiClient(IHttpClientFactory httpClientFactory, AuthViewModel authViewModel)
    {
        _httpClientFactory = httpClientFactory;
        _authViewModel = authViewModel;
    }

    public async Task<HttpResponseMessage> SendAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken = default)
    {
        var response = await SendOnceAsync(requestFactory, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        response.Dispose();
        if (!await _authViewModel.TryRefreshAsync())
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }

        return await SendOnceAsync(requestFactory, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendOnceAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("API");
        using var request = requestFactory();
        var token = await _authViewModel.GetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await client.SendAsync(request, cancellationToken);
    }
}
