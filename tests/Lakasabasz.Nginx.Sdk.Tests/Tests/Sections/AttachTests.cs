namespace Lakasabasz.Nginx.Sdk.Tests.Sections;

using Server = Lakasabasz.Nginx.Sdk.Server;

[NginxRequired(Mode.ReadOnly)]
public sealed class AttachTests : TestTemplate
{
    private NginxFile _site;

    #region SetUp

    [SetUp]
    public void CreateSite() => _site = NewSite(Consts.Sections.SiteName);

    #endregion

    #region Helpers

    private static NginxFile NewSite(string name) => Nginx.Init().Config.SitesAvailable.Create(name);

    private static Server LooseServer()
    {
        var server = new Server();
        server.Directives.Add(Consts.Sections.ServerName, Consts.Sections.AttachName);
        server.Directives.Add(Consts.Sections.Listen, Consts.Sections.AttachPort);
        server.Locations.Add(Consts.Sections.RootPath).Directives.Add(Consts.Sections.Root, Consts.Sections.WebRoot);
        return server;
    }

    #endregion

    [Test]
    [Description("A server made in code is added to a file as the same object: the file lists it and writes it with its locations, and nginx accepts it.")]
    public async Task AddKeepsObject()
    {
        var server = LooseServer();

        var added = _site.Servers.Add(server);
        var result = await NginxControl.TestAsync(_site, WorkDirectory);

        Assert.Multiple(() =>
        {
            Assert.That(added, Is.SameAs(server), "Add should return the added object");
            Assert.That(_site.Servers.Single(), Is.SameAs(server), "The file should list the same object");
            Assert.That(_site.Serialize(), Is.EqualTo(Expected.AttachAdded), "The file should hold the server with its location");
            Assert.That(result.Success, Is.True, Consts.Messages.NginxRejected(result));
        });
    }

    [Test]
    [Description("Changes of an added server and of its location after the file was written change only those lines.")]
    public void ChangesAfterAdd()
    {
        var server = _site.Servers.Add(LooseServer());
        _site.Serialize();

        server.Directives.Set(Consts.Sections.ServerName, Consts.Sections.AttachName, Consts.Sections.AttachAlias);
        server.Locations.Single().Directives.Set(Consts.Sections.Root, Consts.Sections.OtherWebRoot);

        Assert.That(_site.Serialize(), Is.EqualTo(Expected.AttachChanged), "The name and the root should change in place, nothing added twice");
    }

    [Test]
    [Description("A server that is in a file cannot be added again, to the same or to another file.")]
    public void AddTwiceThrows()
    {
        var server = _site.Servers.Add(LooseServer());
        var other = NewSite(Consts.Sections.OtherSiteName);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() => _site.Servers.Add(server), "Adding the server to its own file again should fail");
            Assert.Throws<InvalidOperationException>(() => other.Servers.Add(server), "Adding the server to another file should fail");
            Assert.That(other.Servers, Is.Empty, "The other file should stay without servers");
        });
    }

    [Test]
    [Description("Remove() on a section made in code and not added anywhere throws: it is in no file.")]
    public void LooseRemoveThrows()
    {
        var server = LooseServer();

        Assert.Throws<InvalidOperationException>(server.Remove, "Removing a loose server should fail");
    }

    [Test]
    [Description("A removed server leaves its file and can be added to another one, as the same object.")]
    public void RemoveDetaches()
    {
        var server = _site.Servers.Add(LooseServer());
        var other = NewSite(Consts.Sections.OtherSiteName);

        server.Remove();
        other.Servers.Add(server);

        Assert.Multiple(() =>
        {
            Assert.That(_site.Servers, Is.Empty, "The first file should have no server");
            Assert.That(_site.Serialize(), Is.Empty, "The first file should be empty again");
            Assert.That(other.Servers.Single(), Is.SameAs(server), "The other file should hold the same server");
            Assert.That(other.Serialize(), Is.EqualTo(Expected.AttachAdded), "The other file should hold the whole server");
        });
    }

    [Test]
    [Description("Servers.Remove(server) removes a server of the collection and returns false for one that is not in it.")]
    public void CollectionRemove()
    {
        var server = _site.Servers.Add(LooseServer());

        var first = _site.Servers.Remove(server);
        var second = _site.Servers.Remove(server);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.True, "The server should be removed");
            Assert.That(second, Is.False, "A server no longer in the file should not be removed again");
            Assert.That(_site.Servers, Is.Empty, "The file should have no server");
        });
    }
}
