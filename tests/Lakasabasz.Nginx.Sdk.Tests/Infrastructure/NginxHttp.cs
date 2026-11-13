namespace Lakasabasz.Nginx.Sdk.Tests.Infrastructure;

public static class NginxHttp
{
    private static readonly HttpClient Client = new() { DefaultRequestHeaders = { ConnectionClose = true } };

    public static async Task<string> GetHealthAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(Consts.Http.BaseAddress), Consts.Http.HealthPath));
        request.Headers.Host = Consts.Http.ReportsHost;
        using var response = await Client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public static async Task<string> GetAsync(int port, string path, CancellationToken cancellationToken)
    {
        using var response = await Client.GetAsync(Consts.Http.Url(port, path), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public static async Task<string?> TryGetAsync(int port, string path, CancellationToken cancellationToken)
    {
        try
        {
            return await GetAsync(port, path, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public static async Task EventuallyAsync(Func<Task<bool>> condition, string message, CancellationToken cancellationToken)
    {
        for (var i = 0; i < 50; i++)
        {
            if (await condition())
                return;
            await Task.Delay(100, cancellationToken);
        }
        Assert.Fail(Consts.Messages.ConditionNotMet + message);
    }
}
