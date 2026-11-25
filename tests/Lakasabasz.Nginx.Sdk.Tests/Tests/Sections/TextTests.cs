namespace Lakasabasz.Nginx.Sdk.Tests.Sections;

using Server = Lakasabasz.Nginx.Sdk.Server;

[NginxRequired(Mode.ReadOnly)]
public sealed class TextTests : TestTemplate
{
    private NginxFile _site;

    #region SetUp

    [SetUp]
    public void CreateSite() => _site = Nginx.Init().Config.SitesAvailable.Create(Consts.Sections.SiteName);

    #endregion

    #region Helpers

    private static IReadOnlyList<string> ArgumentsOf(Section section, string name) =>
        section.Directives.Single(d => d.Name == name).Arguments;

    #endregion

    [Test]
    [Description("Servers.AddFromText adds a server made of the text, with its nested blocks, readable as directives; nginx accepts it.")]
    public async Task AddsServer()
    {
        var server = _site.Servers.AddFromText(Consts.Sections.ServerText);

        var result = await NginxControl.TestAsync(_site, WorkDirectory);

        Assert.Multiple(() =>
        {
            Assert.That(ArgumentsOf(server, Consts.Sections.ServerName), Is.EqualTo(Consts.Sections.TextNames), "The server's names should come from the text");
            Assert.That(ArgumentsOf(server.Locations.Single(), Consts.Sections.ProxyPass), Is.EqualTo(Consts.Sections.BackendUrls), "The location of the text should be read");
            Assert.That(server.Directive.Line, Is.Zero, "A server added from text has no line in the file");
            Assert.That(_site.Serialize(), Is.EqualTo(Expected.TextServer), "The text should be written as a server block");
            Assert.That(result.Success, Is.True, Consts.Messages.NginxRejected(result));
        });
    }

    [Test]
    [Description("Locations.AddFromText and Upstreams.AddFromText add a location and an upstream with their keys and the text as content.")]
    public void AddsLocationAndUpstream()
    {
        var upstream = _site.Upstreams.AddFromText(Consts.Sections.UpstreamName, Consts.Sections.UpstreamText);
        var server = _site.Servers.Add();
        var location = server.Locations.AddFromText(LocationModifier.Exact, Consts.Sections.HealthPath, Consts.Sections.LocationText);

        Assert.Multiple(() =>
        {
            Assert.That(ArgumentsOf(upstream, Consts.Sections.UpstreamServer), Is.EqualTo(Consts.Sections.BackendAddresses), "The upstream's server should come from the text");
            Assert.That(location.Modifier, Is.EqualTo(LocationModifier.Exact), "The location should match exactly");
            Assert.That(location.Directives.Single().Name, Is.EqualTo(Consts.Sections.Return), "The location should hold the directive of the text");
            Assert.That(_site.Serialize(), Is.EqualTo(Expected.TextLocationAndUpstream), "Both blocks should be written with their content");
        });
    }

    [Test]
    [Description("A syntax error in the text throws NginxParseException at its line and column in the text, and nothing is added.")]
    public void SyntaxErrorThrows()
    {
        var exception = Assert.Throws<NginxParseException>(() => _site.Servers.AddFromText(Consts.Sections.BrokenText), "Broken text should fail");
        var error = exception.Errors.Single();

        Assert.Multiple(() =>
        {
            Assert.That((error.Line, error.Column), Is.EqualTo(Consts.Sections.BrokenTextPosition), "The error should be at its place in the text");
            Assert.That(error.Message, Is.EqualTo(Consts.Sections.BrokenTextMessage), "The error should be nginx's message");
            Assert.That(_site.Servers, Is.Empty, "No server should be added");
        });
    }
}
