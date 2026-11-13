namespace Lakasabasz.Nginx.Sdk.Tests.Config;

public sealed class DirectiveTests : TestTemplate
{
    private NginxFile _site;

    #region SetUp

    [SetUp]
    public void LoadSite() => _site = Load(Arrange.SiteConfig());

    #endregion

    #region Helpers

    private static NginxFile Load(string path) => Nginx.Load(path).Config.All.Single();

    private Directive SiteServer => _site.Directives.Single(d => d.Name == Consts.Parsing.Server);

    #endregion

    [Test]
    [Description("Directives.Add appends the directive after the last one of the block, on a new line indented like its siblings.")]
    public void AddAppends()
    {
        SiteServer.Directives.Add(Consts.Parsing.ClientMaxBodySize, Consts.Parsing.ClientMaxBodySizeArguments);

        Assert.That(_site.Serialize(), Is.EqualTo(Expected.SiteAdded), "Only the added line should be new; the rest of the file should be unchanged");
    }

    [Test]
    [Description("Directives.Set changes the arguments of the directive in place and keeps the comment after it.")]
    public void SetChangesArguments()
    {
        SiteServer.Directives.Set(Consts.Parsing.ServerName, Consts.Parsing.NewServerNames);

        Assert.That(_site.Serialize(), Is.EqualTo(Expected.SiteSet), "Only the arguments of server_name should change");
    }

    [Test]
    [Description("Directives.Set on a directive that occurs twice leaves one with the new arguments and removes the other line.")]
    public void SetRemovesDuplicates()
    {
        SiteServer.Directives.Set(Consts.Parsing.AddHeader, Consts.Parsing.NewHeader);

        Assert.That(_site.Serialize(), Is.EqualTo(Expected.SiteSetDuplicates), "The first add_header should change and the second line disappear");
    }

    [Test]
    [Description("Directives.Remove removes the directive's whole line, with its indentation and the comment after it.")]
    public void RemoveDropsLine()
    {
        var removed = SiteServer.Directives.Remove(Consts.Parsing.ServerName);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.EqualTo(1), "One server_name should be removed");
            Assert.That(_site.Serialize(), Is.EqualTo(Expected.SiteRemoved), "Only the server_name line should disappear");
        });
    }

    [Test]
    [Description("Directives.AddBlock adds an empty block that is filled through its own Directives, indented one level deeper.")]
    public void AddBlockNests()
    {
        SiteServer.Directives.AddBlock(Consts.Parsing.Location, Consts.Parsing.ApiLocation)
            .Directives.Add(Consts.Parsing.ProxyPass, Consts.Parsing.ApiProxy);

        Assert.That(_site.Serialize(), Is.EqualTo(Expected.SiteBlock), "The new location should hold its proxy_pass, indented inside the server");
    }

    [Test]
    [Description("An added argument that nginx could not read unquoted (spaces, ; { }) is written in double quotes.")]
    public void QuotesWhenNeeded()
    {
        SiteServer.Directives.Add(Consts.Parsing.AddHeader, Consts.Parsing.QuotedHeader);

        Assert.That(_site.Serialize(), Is.EqualTo(Expected.SiteQuoted), "The header value with spaces and ;{} should be quoted, its name not");
    }

    [Test]
    [Description("Directives.Add on the file adds a top-level directive after the last one, before the final line break.")]
    public void AddsTopLevel()
    {
        _site.Directives.Add(Consts.Parsing.User, Consts.Parsing.UserArguments);

        Assert.That(_site.Serialize(), Is.EqualTo(Expected.SiteTopLevel), "user should be added on its own line at the end of the file");
    }

    [Test]
    [Description("A directive added to a block written on one line stays on that line.")]
    public void KeepsInlineBlocks()
    {
        var file = Load(Arrange.InlineConfig());

        file.Directives.Single().Directives.Add(Consts.Parsing.MultiAccept, Consts.Parsing.On);

        Assert.That(file.Serialize(), Is.EqualTo(Expected.InlineAdded), "multi_accept should be added inside the one-line events block");
    }

    [Test]
    [Description("A directive added to a file with CRLF line endings gets a CRLF line ending too.")]
    public void KeepsCrlf()
    {
        var file = Load(Arrange.CrlfConfig());

        file.Directives.Single().Directives.Add(Consts.Parsing.MultiAccept, Consts.Parsing.On);

        Assert.That(file.Serialize(), Is.EqualTo(Expected.CrlfAdded), "The new line should end with CRLF like the others");
    }

    [Test]
    [Description("A simple directive has no block: adding a directive into it throws InvalidOperationException.")]
    public void SimpleDirectiveHasNoBlock()
    {
        var listen = SiteServer.Directives[0];

        Assert.Throws<InvalidOperationException>(() => listen.Directives.Add(Consts.Parsing.User, Consts.Parsing.UserArguments),
            "A simple directive like listen should not accept directives inside it");
    }
}
