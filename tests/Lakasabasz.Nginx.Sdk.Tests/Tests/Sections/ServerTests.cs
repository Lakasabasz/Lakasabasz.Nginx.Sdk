namespace Lakasabasz.Nginx.Sdk.Tests.Sections;

using Server = Lakasabasz.Nginx.Sdk.Server;

public sealed class ServerTests : TestTemplate
{
    private NginxFile _file;

    #region SetUp

    [SetUp]
    public void LoadNames() => _file = NginxFile.Load(Arrange.NamesConfig());

    #endregion

    #region Helpers

    private Server Server => _file.Servers.Single();

    #endregion

    [Test]
    [Description("Names lists the names of every server_name directive, in order, and Contains compares them as written.")]
    public void NamesFromEveryDirective()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Server.Names, Is.EqualTo(Consts.Sections.FileNames), "Names should list the names of both server_name lines");
            Assert.That(Server.Names.Contains(Consts.Sections.WildcardName), Is.True, "A wildcard should be found as written");
            Assert.That(Server.Names.Contains(Consts.Sections.MatchedByWildcard), Is.False, "A name the wildcard matches should not be found");
        });
    }

    [Test]
    [Description("Changing Names changes the directive holding the name: a new name goes to the last server_name, a line left empty is removed.")]
    public void NamesChangeInPlace()
    {
        Server.Names.Add(Consts.Sections.AddedName);
        Server.Names.Remove(Consts.Sections.FirstLineName);
        Server.Names[0] = Consts.Sections.RenamedName;

        Assert.That(_file.Serialize(), Is.EqualTo(Expected.NamesChanged), "Only the server_name lines should change");
    }

    [Test]
    [Description("Listens reads every form of the listen address: a port, an address with a port, IPv6, a host name and a unix socket.")]
    public void ListensReadAddresses()
    {
        Assert.That(Server.Listens, Is.EqualTo(Consts.Sections.FileListens), "Each listen should give its address, port and flags");
    }

    [Test]
    [Description("Ports, SslPorts and IsDefault are derived from the listens: 80 for a listen without a port, none for a unix socket.")]
    public void DerivedFromListens()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Server.Ports, Is.EqualTo(Consts.Sections.FilePorts), "Ports should list each port once");
            Assert.That(Server.SslPorts, Is.EqualTo(Consts.Sections.FileSslPorts), "SslPorts should list the ports of the ssl listens");
            Assert.That(Server.IsDefault, Is.False, "No listen is default_server");
        });
    }

    [Test]
    [Description("A listen replaced in Listens rewrites only its own line, and default_server makes the server the default one.")]
    public void ListenReplaced()
    {
        Server.Listens[0] = Server.Listens[0] with { Port = Consts.Sections.ReplacedPort, DefaultServer = true };

        Assert.Multiple(() =>
        {
            Assert.That(Server.IsDefault, Is.True, "The server should be the default one");
            Assert.That(Server.Ports, Does.Contain(Consts.Sections.ReplacedPort), "The new port should be listed");
            Assert.That(_file.Serialize(), Is.EqualTo(Expected.ListenReplaced), "Only the first listen line should change");
        });
    }

    [Test]
    [Description("Inserting a listen before another is not possible, as directives are only added at the end; adding at the end is.")]
    public void ListenInsertAtEnd()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<NotSupportedException>(() => Server.Listens.Insert(0, Consts.Sections.SslListen), "Inserting before the first listen should fail");
            Assert.That(() => Server.Listens.Insert(Server.Listens.Count, Consts.Sections.SslListen), Throws.Nothing, "Inserting at the end should add");
        });
    }
}
