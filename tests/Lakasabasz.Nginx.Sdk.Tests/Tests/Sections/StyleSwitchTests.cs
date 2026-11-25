namespace Lakasabasz.Nginx.Sdk.Tests.Sections;

using Server = Lakasabasz.Nginx.Sdk.Server;

[NginxRequired(Mode.ReadOnly)]
public sealed class StyleSwitchTests : TestTemplate
{
    private NginxFile _site;

    #region SetUp

    [SetUp]
    public void CreateSite() => _site = Nginx.Init().Config.SitesAvailable.Create(Consts.Sections.SiteName);

    #endregion

    #region Helpers

    private Server BuildWithProperties()
    {
        var server = _site.Servers.Add();
        server.Listens.Add(new Listen { Port = Consts.Sections.SwitchPort });
        server.Names.Add(Consts.Sections.SwitchName);
        server.Index.Add(Consts.Sections.IndexHtml);
        return server;
    }

    private Directive BuildWithDirectives()
    {
        var server = _site.Directives.AddBlock(Consts.Sections.ServerBlock);
        server.Directives.Add(Consts.Sections.Listen, Consts.Sections.SwitchListen);
        server.Directives.Add(Consts.Sections.ServerName, Consts.Sections.SwitchName);
        server.Directives.Add(Consts.Sections.Index, Consts.Sections.IndexHtml);
        return server;
    }

    private Server BuildWithText() => _site.Servers.AddFromText(Consts.Sections.SwitchText);

    private static void AddApiWithProperties(Server server) =>
        server.Locations.Add(Consts.Sections.ApiPath).ProxyPass = Consts.Sections.BackendUrl;

    private static void AddApiWithDirectives(Directive server) => server.Directives
        .AddBlock(Consts.Sections.LocationBlock, Consts.Sections.ApiPath)
        .Directives.Add(Consts.Sections.ProxyPass, Consts.Sections.BackendUrl);

    private static void AddApiWithText(Server server) => server.Locations.AddFromText(Consts.Sections.ApiPath, Consts.Sections.ApiText);

    private async Task AssertSwitchedAsync(string read)
    {
        var result = await NginxControl.TestAsync(Arrange.MainConfig(_site.Serialize()));

        Assert.Multiple(() =>
        {
            Assert.That(read, Is.EqualTo(Consts.Sections.SwitchName), "The name written in one style should be read in the other");
            Assert.That(_site.Serialize(), Is.EqualTo(Expected.Switched), "Every pair of styles should give the same file");
            Assert.That(result.Success, Is.True, Consts.Messages.NginxRejected(result));
        });
    }

    #endregion

    [Test]
    [Description("A server built with properties is read and extended with generic directives.")]
    public async Task PropertiesToDirectives()
    {
        BuildWithProperties();
        var server = _site.Directives.Single();

        AddApiWithDirectives(server);

        await AssertSwitchedAsync(server.Directives.Single(d => d.Name == Consts.Sections.ServerName).Arguments.Single());
    }

    [Test]
    [Description("A server built with properties is extended with nginx text.")]
    public async Task PropertiesToText()
    {
        var server = BuildWithProperties();

        AddApiWithText(server);

        await AssertSwitchedAsync(server.Names.Single());
    }

    [Test]
    [Description("A server built with generic directives is read and extended with properties.")]
    public async Task DirectivesToProperties()
    {
        BuildWithDirectives();
        var server = _site.Servers.Single();

        AddApiWithProperties(server);

        await AssertSwitchedAsync(server.Names.Single());
    }

    [Test]
    [Description("A server built with generic directives is extended with nginx text.")]
    public async Task DirectivesToText()
    {
        var directive = BuildWithDirectives();

        AddApiWithText(_site.Servers.Single());

        await AssertSwitchedAsync(directive.Directives.Single(d => d.Name == Consts.Sections.ServerName).Arguments.Single());
    }

    [Test]
    [Description("A server added as nginx text is read and extended with properties.")]
    public async Task TextToProperties()
    {
        var server = BuildWithText();

        AddApiWithProperties(server);

        await AssertSwitchedAsync(server.Names.Single());
    }

    [Test]
    [Description("A server added as nginx text is read and extended with generic directives.")]
    public async Task TextToDirectives()
    {
        var server = BuildWithText();

        AddApiWithDirectives(server.Directive);

        await AssertSwitchedAsync(server.Directive.Directives.Single(d => d.Name == Consts.Sections.ServerName).Arguments.Single());
    }

    [Test]
    [Description("An index directive written with generic directives or as nginx text is read and changed through the Index property.")]
    public void IndexByProperties()
    {
        BuildWithDirectives();
        var fromDirectives = _site.Servers.Single();
        var fromText = BuildWithText();

        fromDirectives.Index.Add(Consts.Sections.IndexHtm);
        fromText.Index.Add(Consts.Sections.IndexHtm);

        Assert.Multiple(() =>
        {
            Assert.That(fromDirectives.Index, Is.EqualTo(new[] { Consts.Sections.IndexHtml, Consts.Sections.IndexHtm }), "The index of generic directives should be extended");
            Assert.That(fromText.Index, Is.EqualTo(new[] { Consts.Sections.IndexHtml, Consts.Sections.IndexHtm }), "The index of the text should be extended");
            Assert.That(_site.Serialize(), Is.EqualTo(Expected.IndexExtended), "Both index lines should hold the added file");
        });
    }
}
