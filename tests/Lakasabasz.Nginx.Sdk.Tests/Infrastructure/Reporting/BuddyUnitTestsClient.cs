using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lakasabasz.Nginx.Sdk.Tests.Infrastructure.Reporting;

public sealed record BuddyTestCase(
    string Name,
    string TestGroupName,
    string Classname,
    BuddyTestStatus Status,
    double Time,
    string? Data = null);

[JsonConverter(typeof(JsonStringEnumConverter<BuddyTestStatus>))]
public enum BuddyTestStatus
{
    PASSED,
    FAILED,
    SKIPPED,
    ERROR,
}

public sealed record BuddyTestSessionInfo(
    string CiProvider,
    string? Revision = null,
    string? RefType = null,
    string? RefName = null,
    string? ExecutionId = null,
    string? ActionExecutionId = null)
{
    public static BuddyTestSessionInfo From(BuddyRun? run)
    {
        if (run is null)
            return new BuddyTestSessionInfo(Consts.Buddy.NoCiProvider);

        var (refType, refName) = (run.RefType?.ToUpperInvariant(), run.RefName) switch
        {
            ({ } type and (Consts.Buddy.Branch or Consts.Buddy.Tag or Consts.Buddy.PullRequest), { } name) => (type, name),
            _ when run.Tag is not null => (Consts.Buddy.Tag, run.Tag),
            _ when run.Branch is not null => (Consts.Buddy.Branch, run.Branch),
            _ => ((string?)null, (string?)null),
        };

        return new BuddyTestSessionInfo(Consts.Buddy.CiProvider, run.Commit, refType, refName, run.ExecutionId, run.ActionExecutionId);
    }
}

public sealed class BuddyUnitTestsClient : IDisposable
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;

    public BuddyUnitTestsClient(string token, HttpMessageHandler? handler = null)
    {
        _http = handler is null ? new HttpClient() : new HttpClient(handler);
        _http.BaseAddress = ServiceUrl(token);
        _http.Timeout = TimeSpan.FromSeconds(30);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Consts.Buddy.Bearer, token);
    }

    public static Uri ServiceUrl(string token)
    {
        if (TestConfiguration.BuddyUnitTestsServiceUrl is { } custom)
            return custom;

        var region = token.Split(Consts.Buddy.TokenSeparator) is [_, Consts.Buddy.TokenKind, var r, ..] ? r : Consts.Buddy.NoRegion;
        return new Uri(region switch
        {
            Consts.Buddy.EuRegion => Consts.Buddy.EuUrl,
            Consts.Buddy.AsiaRegion => Consts.Buddy.AsiaUrl,
            _ => Consts.Buddy.DefaultUrl,
        });
    }

    public async Task<string> OpenSessionAsync(BuddyTestSessionInfo info, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync(Consts.Buddy.SessionsPath, info, Json, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var session = await response.Content.ReadFromJsonAsync<JsonElement>(Json, cancellationToken);
        return session.GetProperty(Consts.Buddy.IdProperty).GetString()!;
    }

    public async Task AddCasesAsync(string sessionId, IReadOnlyCollection<BuddyTestCase> cases, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync(Consts.Buddy.CasesPath(sessionId), new { cases }, Json, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task CloseSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync(Consts.Buddy.ClosePath(sessionId), content: null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public void Dispose() => _http.Dispose();

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(Consts.Buddy.RequestFailed(response, body));
        }
    }
}
