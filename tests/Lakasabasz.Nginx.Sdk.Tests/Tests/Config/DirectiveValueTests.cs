namespace Lakasabasz.Nginx.Sdk.Tests.Config;

[NginxRequired(Mode.Modify)]
public sealed class DirectiveValueTests : TestTemplate
{
    private string _valuesSite;

    #region SetUp

    [SetUp]
    public async Task ServeValuesSiteAsync()
    {
        _valuesSite = Arrange.ValuesSite();
        Nginx.Init(sudo: true).Server.Reload();
        await NginxHttp.EventuallyAsync(
            async () => await NginxHttp.TryGetAsync(Consts.Parsing.ValuesPort, Consts.Parsing.PlainLocation, Cancellation) is not null,
            "nginx should serve the values site after the reload",
            Cancellation);
    }

    #endregion

    #region Helpers

    private static IEnumerable<Directive> Locations(NginxFile file) => file.Directives
        .Single(d => d.Name == Consts.Parsing.Server).Directives
        .Where(d => d.Name == Consts.Parsing.Location);

    #endregion

    [Test]
    [Description("Every argument the package reads from a config equals the value nginx serves from it: plain, quoted, escaped, with ; { } #.")]
    public async Task ArgumentsMatchNginx()
    {
        var returns = Locations(Nginx.Load(_valuesSite).Config.All.Single())
            .Select(location => (Path: location.Arguments[1], Body: location.Directives.Single().Arguments[1]))
            .ToList();

        var served = new List<string>();
        foreach (var (location, _) in returns)
            served.Add(await NginxHttp.GetAsync(Consts.Parsing.ValuesPort, location, Cancellation));

        Assert.That(served, Is.EqualTo(returns.Select(r => r.Body)), "nginx should serve exactly the argument values the package read");
    }

    [Test]
    [Description("An argument set by the package with quotes, a backslash, ; { } and # is served by nginx exactly as set.")]
    public async Task SetArgumentsMatchNginx()
    {
        var file = Nginx.Load(_valuesSite).Config.All.Single();
        Locations(file).Single(location => location.Arguments[1] == Consts.Parsing.PlainLocation)
            .Directives.Set(Consts.Parsing.Return, Consts.Parsing.TrickyStatusAndBody);
        await File.WriteAllTextAsync(_valuesSite, file.Serialize(), Cancellation);

        Nginx.Init(sudo: true).Server.Reload();

        await NginxHttp.EventuallyAsync(
            async () => await NginxHttp.TryGetAsync(Consts.Parsing.ValuesPort, Consts.Parsing.PlainLocation, Cancellation) == Consts.Parsing.TrickyBody,
            "nginx should serve the value set by the package unchanged",
            Cancellation);
    }
}
