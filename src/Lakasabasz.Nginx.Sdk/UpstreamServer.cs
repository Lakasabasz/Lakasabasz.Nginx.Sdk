using System.Globalization;
using Lakasabasz.Nginx.Sdk.Internal.Structure;
using Lakasabasz.Nginx.Sdk.Views;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// One <c>server</c> of an upstream, e.g. <c>server 127.0.0.1:5000 weight=2 max_fails=3 fail_timeout=30s;</c>.
/// Immutable: to change one, set a new value in <see cref="Upstream.Servers"/>.
/// </summary>
/// <param name="Address">The address with the port, e.g. <c>127.0.0.1:5000</c>, <c>backend.local</c> or <c>unix:/run/app.sock</c>.</param>
public sealed record UpstreamServer(string Address)
{
    private const string WeightParameter = "weight=";
    private const string MaxConnectionsParameter = "max_conns=";
    private const string MaxFailsParameter = "max_fails=";
    private const string FailTimeoutParameter = "fail_timeout=";
    private const string BackupFlag = "backup";
    private const string DownFlag = "down";

    /// <summary>The share of requests (<c>weight=</c>); <see langword="null"/> when not given (1).</summary>
    public int? Weight { get; init; }

    /// <summary>The most connections at once (<c>max_conns=</c>); <see langword="null"/> when not given (no limit).</summary>
    public int? MaxConnections { get; init; }

    /// <summary>Failed attempts after which the server is unavailable (<c>max_fails=</c>); <see langword="null"/> when not given (1).</summary>
    public int? MaxFails { get; init; }

    /// <summary>The time the failures are counted in and the server is then unavailable (<c>fail_timeout=</c>).</summary>
    public TimeSpan? FailTimeout { get; init; }

    /// <summary>Used only when the other servers are unavailable (<c>backup</c>).</summary>
    public bool Backup { get; init; }

    /// <summary>Marked as unavailable (<c>down</c>).</summary>
    public bool Down { get; init; }

    /// <summary>The other parameters, as written, e.g. <c>resolve</c>.</summary>
    public IReadOnlyList<string> Parameters { get; init; } = [];

    /// <inheritdoc />
    public bool Equals(UpstreamServer? other) => other is not null
        && Address == other.Address && Weight == other.Weight && MaxConnections == other.MaxConnections
        && MaxFails == other.MaxFails && FailTimeout == other.FailTimeout && Backup == other.Backup && Down == other.Down
        && Parameters.SequenceEqual(other.Parameters);

    /// <inheritdoc />
    public override int GetHashCode() => (Address, Weight, Backup, Down).GetHashCode();

    internal static UpstreamServer Read(StructureDirective directive)
    {
        if (directive.Arguments.Count == 0)
            throw DirectiveValues.NotA(directive, "an address");

        var server = new UpstreamServer(directive.Arguments[0]);
        var parameters = new List<string>();
        foreach (var argument in directive.Arguments.Skip(1))
        {
            if (Read(server, argument) is { } read)
                server = read;
            else
                parameters.Add(argument);
        }
        return server with { Parameters = parameters };
    }

    internal IReadOnlyList<string> Arguments()
    {
        var arguments = new List<string> { Address };
        if (Weight is { } weight)
            arguments.Add(WeightParameter + Number(weight));
        if (MaxConnections is { } maxConnections)
            arguments.Add(MaxConnectionsParameter + Number(maxConnections));
        if (MaxFails is { } maxFails)
            arguments.Add(MaxFailsParameter + Number(maxFails));
        if (FailTimeout is { } failTimeout)
            arguments.Add(FailTimeoutParameter + NginxTime.Format(failTimeout));
        if (Backup)
            arguments.Add(BackupFlag);
        if (Down)
            arguments.Add(DownFlag);
        arguments.AddRange(Parameters);
        return arguments;
    }

    /// <summary>The server with one more known parameter; <see langword="null"/> for a parameter it does not know.</summary>
    private static UpstreamServer? Read(UpstreamServer server, string argument) => argument switch
    {
        BackupFlag => server with { Backup = true },
        DownFlag => server with { Down = true },
        _ when ValueOf(argument, WeightParameter) is { } text && DirectiveValues.TryParseNumber(text, out var weight) => server with { Weight = weight },
        _ when ValueOf(argument, MaxConnectionsParameter) is { } text && DirectiveValues.TryParseNumber(text, out var max) => server with { MaxConnections = max },
        _ when ValueOf(argument, MaxFailsParameter) is { } text && DirectiveValues.TryParseNumber(text, out var fails) => server with { MaxFails = fails },
        _ when ValueOf(argument, FailTimeoutParameter) is { } text && NginxTime.TryParse(text, out var timeout) => server with { FailTimeout = timeout },
        _ => null,
    };

    private static string? ValueOf(string argument, string parameter) =>
        argument.StartsWith(parameter, StringComparison.Ordinal) ? argument.Substring(parameter.Length) : null;

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
