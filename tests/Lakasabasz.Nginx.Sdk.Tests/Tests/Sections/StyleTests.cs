namespace Lakasabasz.Nginx.Sdk.Tests.Sections;

using Server = Lakasabasz.Nginx.Sdk.Server;

[NginxRequired(Mode.ReadOnly)]
public sealed class StyleTests : TestTemplate
{
    private NginxFile _file;

    #region SetUp

    [SetUp]
    public void LoadMixed() => _file = NginxFile.Load(Arrange.MixedConfig());

    #endregion

    #region Helpers

    private Server Server => _file.Servers.Single();

    private Location OneLine => Server.Locations.First();

    private Location MultiLine => Server.Locations.Last();

    private Task<NginxProcessResult> NginxTestAsync() => NginxControl.TestAsync(_file, WorkDirectory);

    #endregion

    [Test]
    [Description("The file's style is read from it (CRLF, tabs, the most common braces and spacing), each block's braces too; a new file gets the default.")]
    public void ReadsStyles()
    {
        var created = Nginx.Init().Config.SitesAvailable.Create(Consts.Sections.SiteName);

        Assert.Multiple(() =>
        {
            Assert.That(_file.Style, Is.EqualTo(Consts.Sections.MixedStyle), "CRLF, tabs, braces on a new line and no spacing should be read");
            Assert.That(created.Style, Is.EqualTo(NginxStyle.Default), "A new file should get the default style");
            Assert.That(Server.Style.Braces, Is.EqualTo(BraceStyle.NewLine), "The server's brace should be on a line of its own");
            Assert.That(OneLine.Style.Braces, Is.EqualTo(BraceStyle.OneLine), "The first location should be on one line");
            Assert.That(MultiLine.Style.Braces, Is.EqualTo(BraceStyle.EndOfLine), "The second location should have its brace at the end of the line");
        });
    }

    [Test]
    [Description("The width of the indentation is read from the step between a block and the directives in it, not assumed.")]
    public void ReadsIndentationWidth()
    {
        var twoSpaces = NginxFile.Load(Arrange.TwoSpacesConfig());

        Assert.That(twoSpaces.Style.Indentation, Is.EqualTo(Consts.Sections.TwoSpaces), "A file indented by two spaces should be read as such");
    }

    [Test]
    [Description("A change of one directive writes it in the file's style; the rest of the mixed file stays byte for byte.")]
    public async Task KeepsUnchanged()
    {
        MultiLine.Root = Consts.Sections.OtherWebRoot;
        Server.Locations.Add(Consts.Sections.ApiPath).ProxyPass = Consts.Sections.BackendUrl;

        var result = await NginxTestAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_file.Serialize(), Is.EqualTo(Expected.MixedChanged), "Only the root and the new location should be new text, in CRLF and tabs");
            Assert.That(result.Success, Is.True, Consts.Messages.NginxRejected(result));
        });
    }

    [Test]
    [Description("A style set on the file is used for the new text: line break, indentation, braces and spacing before blocks.")]
    public async Task SetsFileStyle()
    {
        _file.Style = Consts.Sections.RelaidStyle;

        _file.Servers.Add().Locations.Add(Consts.Sections.ApiPath).ProxyPass = Consts.Sections.BackendUrl;

        var result = await NginxTestAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_file.Serialize(), Is.EqualTo(Expected.MixedRelaid), "The new server should use LF, two spaces, braces at the end of the line and two empty lines");
            Assert.That(result.Success, Is.True, Consts.Messages.NginxRejected(result));
        });
    }

    [Test]
    [Description("A block's braces can be changed: a multi-line block onto one line, a one-line block onto lines, a brace onto a line of its own.")]
    public async Task SetsBlockStyle()
    {
        MultiLine.Style = MultiLine.Style with { Braces = BraceStyle.OneLine };
        OneLine.Style = OneLine.Style with { Braces = BraceStyle.EndOfLine };
        Server.Locations.Add(Consts.Sections.ApiPath).Style = Server.Style with { Braces = BraceStyle.NewLine };

        var result = await NginxTestAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_file.Serialize(), Is.EqualTo(Expected.MixedRestyled), "The two locations should swap braces and the new one get its brace on a new line");
            Assert.That(result.Success, Is.True, Consts.Messages.NginxRejected(result));
        });
    }

    [Test]
    [Description("A section made in code takes the style of the file it is added to; one with a style set keeps it.")]
    public async Task LooseTakesFileStyle()
    {
        var loose = new Server { Names = { Consts.Sections.AttachName }, Locations = { new Location(Consts.Sections.RootPath) { Root = Consts.Sections.WebRoot } } };
        var styled = new Server { Style = Consts.Sections.TwoSpaceStyle, Names = { Consts.Sections.AttachAlias } };

        _file.Servers.Add(loose);
        _file.Servers.Add(styled);
        var result = await NginxTestAsync();

        Assert.Multiple(() =>
        {
            Assert.That(loose.Style, Is.EqualTo(_file.Style), "The loose server should take the file's style");
            Assert.That(styled.Style.Indentation, Is.EqualTo(Consts.Sections.TwoSpaces), "The server with a style set should keep its indentation");
            Assert.That(_file.Serialize(), Is.EqualTo(Expected.MixedLooseAdded), "The loose server should be written in tabs and CRLF, the other in two spaces");
            Assert.That(result.Success, Is.True, Consts.Messages.NginxRejected(result));
        });
    }

    [Test]
    [Description("A new block gets the empty lines of the spacing above it, except as the first directive of its block.")]
    public void SpacesBlocks()
    {
        var site = Nginx.Init().Config.SitesAvailable.Create(Consts.Sections.SiteName);
        site.Style = site.Style with { BlockSpacing = Consts.Sections.WideSpacing };

        var server = site.Servers.Add();
        server.Locations.Add(Consts.Sections.RootPath).Root = Consts.Sections.WebRoot;
        server.Names.Add(Consts.Sections.AttachName);
        server.Locations.Add(Consts.Sections.ApiPath).ProxyPass = Consts.Sections.BackendUrl;
        site.Servers.Add().Names.Add(Consts.Sections.AttachAlias);

        Assert.That(site.Serialize(), Is.EqualTo(Expected.Spaced), "Two empty lines should be before each block that follows a directive");
    }

    [Test]
    [Description("A new section made on one line stays on one line; an unknown line break, an indentation of no characters, or a line break of a block other than the file's, is refused.")]
    public void NewOneLineSection()
    {
        var health = new Location(LocationModifier.Exact, Consts.Sections.HealthPath) { Style = _file.Style with { Braces = BraceStyle.OneLine } };
        health.Directives.Add(Consts.Sections.Return, Consts.Sections.NoContent);

        Server.Locations.Add(health);

        Assert.Multiple(() =>
        {
            Assert.That(_file.Serialize(), Is.EqualTo(Expected.MixedOneLineAdded), "The new location should be written on one line");
            Assert.Throws<ArgumentException>(() => _file.Style = _file.Style with { LineBreak = Consts.Sections.UnknownLineBreak }, "An unknown line break should be refused");
            Assert.Throws<ArgumentOutOfRangeException>(() => Indentation.Spaces(Consts.Sections.NoWidth), "An indentation of no spaces should be refused");
            Assert.Throws<ArgumentException>(() => _file.Style = _file.Style with { BlockSpacing = -1 }, "Spacing cannot be negative");
            Assert.Throws<ArgumentException>(() => Server.Style = Server.Style with { LineBreak = LineBreak.Lf }, "A block's line break is the file's");
        });
    }
}
