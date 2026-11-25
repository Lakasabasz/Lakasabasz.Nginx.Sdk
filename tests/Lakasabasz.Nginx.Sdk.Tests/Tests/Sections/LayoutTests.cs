namespace Lakasabasz.Nginx.Sdk.Tests.Sections;

using Server = Lakasabasz.Nginx.Sdk.Server;

[NginxRequired(Mode.ReadOnly)]
public sealed class LayoutTests : TestTemplate
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
    [Description("The file's layout is read from it (CRLF, tabs), each block's layout too (brace on a new line, one line); a new file gets the default.")]
    public void ReadsLayouts()
    {
        var created = Nginx.Init().Config.SitesAvailable.Create(Consts.Sections.SiteName);

        Assert.Multiple(() =>
        {
            Assert.That(_file.Layout, Is.EqualTo(new NginxLayout { Newline = Consts.Sections.Crlf, Indentation = Consts.Sections.Tab }), "CRLF and tabs should be read");
            Assert.That(created.Layout, Is.EqualTo(NginxLayout.Default), "A new file should get the default layout");
            Assert.That(Server.Layout, Is.EqualTo(new BlockLayout { BraceOnNewLine = true }), "The server's brace should be on a line of its own");
            Assert.That(OneLine.Layout, Is.EqualTo(new BlockLayout { OneLine = true }), "The first location should be on one line");
            Assert.That(MultiLine.Layout, Is.EqualTo(BlockLayout.Default), "The second location should have the default layout");
        });
    }

    [Test]
    [Description("A change of one directive writes it in the file's layout; the rest of the mixed file stays byte for byte.")]
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
    [Description("A layout set on the file is used for the new text: line break and indentation.")]
    public async Task SetsFileLayout()
    {
        _file.Layout = new NginxLayout { Newline = Consts.Sections.Lf, Indentation = Consts.Sections.TwoSpaces };

        Server.Locations.Add(Consts.Sections.ApiPath).ProxyPass = Consts.Sections.BackendUrl;

        var result = await NginxTestAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_file.Serialize(), Is.EqualTo(Expected.MixedRelaid), "The new location should use LF and two spaces, the rest unchanged");
            Assert.That(result.Success, Is.True, Consts.Messages.NginxRejected(result));
        });
    }

    [Test]
    [Description("A block's layout can be changed: a multi-line block onto one line, a one-line block onto lines, a brace onto a line of its own.")]
    public async Task SetsBlockLayout()
    {
        MultiLine.Layout = new BlockLayout { OneLine = true };
        OneLine.Layout = BlockLayout.Default;
        Server.Locations.Add(Consts.Sections.ApiPath).Layout = new BlockLayout { BraceOnNewLine = true };

        var result = await NginxTestAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_file.Serialize(), Is.EqualTo(Expected.MixedRestyled), "The two locations should swap layouts and the new one get its brace on a new line");
            Assert.That(result.Success, Is.True, Consts.Messages.NginxRejected(result));
        });
    }

    [Test]
    [Description("A new section made on one line stays on one line; an invalid file layout is refused.")]
    public void NewOneLineSection()
    {
        var health = new Location(LocationModifier.Exact, Consts.Sections.HealthPath) { Layout = new BlockLayout { OneLine = true } };
        health.Directives.Add(Consts.Sections.Return, Consts.Sections.NoContent);

        Server.Locations.Add(health);

        Assert.Multiple(() =>
        {
            Assert.That(_file.Serialize(), Is.EqualTo(Expected.MixedOneLineAdded), "The new location should be written on one line");
            Assert.Throws<ArgumentException>(() => _file.Layout = new NginxLayout { Newline = Consts.Sections.Tab }, "A tab is not a line break");
        });
    }
}
