namespace Lakasabasz.Nginx.Sdk.Tests.Sections;

using Server = Lakasabasz.Nginx.Sdk.Server;

[NginxRequired(Mode.ReadOnly)]
public sealed class FileSectionsTests : TestTemplate
{
    private Nginx _nginx;

    #region SetUp

    [SetUp]
    public void LoadMain()
    {
        Arrange.Certificate();
        _nginx = Nginx.Load(Arrange.MainSectionsConfig());
    }

    #endregion

    #region Helpers

    private NginxFile Main => _nginx.Config.All.Single();

    private static IEnumerable<string> NamesOf(Server server) =>
        server.Directives.Where(d => d.Name == Consts.Sections.ServerName).SelectMany(d => d.Arguments);

    private static bool ListensWithSsl(Server server) =>
        server.Directives.Any(d => d.Name == Consts.Sections.Listen && d.Arguments.Contains(Consts.Sections.Ssl));

    #endregion

    [Test]
    [Description("The servers and upstreams of a file include those in its http block, so a search over all files finds every server.")]
    public void ListsSectionsInHttp()
    {
        var sslServers = _nginx.Config.All.SelectMany(f => f.Servers).Where(ListensWithSsl);

        Assert.Multiple(() =>
        {
            Assert.That(sslServers.SelectMany(NamesOf), Is.EqualTo(Consts.Sections.SslServerNames), "The ssl server in http should be found");
            Assert.That(Main.Servers.SelectMany(NamesOf), Is.EqualTo(Consts.Sections.MainServerNames), "The file should list both servers of http");
            Assert.That(Main.Servers, Is.EqualTo(Main.Http!.Servers), "The file's servers should be the same objects as those of http");
            Assert.That(Main.Upstreams.Single().Name, Is.EqualTo(Consts.Sections.UpstreamName), "The upstream in http should be found");
        });
    }

    [Test]
    [Description("A server or upstream added to a file with an http block goes into it, where nginx accepts it; one removed from the file leaves http.")]
    public async Task AddsIntoHttp()
    {
        var server = Main.Servers.Add();
        server.Directives.Add(Consts.Sections.ServerName, Consts.Sections.AddedServerName);
        server.Directives.Add(Consts.Sections.Listen, Consts.Sections.AddedServerPort);
        Main.Upstreams.AddFromText(Consts.Sections.AddedUpstreamName, Consts.Sections.UpstreamText);
        var removed = Main.Servers.Remove(Main.Servers.Single(s => NamesOf(s).Contains(Consts.Sections.PlainServerName)));

        var result = await NginxControl.TestAsync(Main, WorkDirectory);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True, "The server in http should be removed through the file");
            Assert.That(Main.Upstreams.Select(u => u.Name), Is.EqualTo(Consts.Sections.MainUpstreamNames), "Both upstreams should be listed");
            Assert.That(Main.Serialize(), Is.EqualTo(Expected.MainAdded), "The new server and upstream should be inside http");
            Assert.That(result.Success, Is.True, Consts.Messages.NginxRejected(result));
        });
    }
}
