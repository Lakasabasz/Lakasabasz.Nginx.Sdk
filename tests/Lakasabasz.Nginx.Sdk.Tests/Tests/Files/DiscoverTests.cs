namespace Lakasabasz.Nginx.Sdk.Tests.Files;

[NginxRequired(Mode.ReadOnly)]
public sealed class DiscoverTests : TestTemplate
{
    #region Helpers

    private static IEnumerable<string> Names(IEnumerable<NginxFile> files) => files.Select(file => Path.GetFileName(file.Path));

    #endregion

    [Test]
    [Description("Nginx.Discover() loads every file nginx itself loads, as nginx -T lists them; an enabled site as its file in sites-available.")]
    public async Task LoadsWhatNginxLoads()
    {
        var loadedByNginx = await NginxDump.LoadedFilesAsync();

        var nginx = Nginx.Discover();

        Assert.Multiple(() =>
        {
            Assert.That(loadedByNginx, Is.Not.Empty, "nginx -T should list the files nginx loads");
            Assert.That(nginx.Config.All.Select(file => file.Path), Is.SupersetOf(loadedByNginx), "Discover should load every file nginx -T lists");
        });
    }

    [Test]
    [Description("Nginx.Discover() loads every site of sites-available, also the disabled ones nginx does not load.")]
    public void LoadsDisabledSites()
    {
        var nginx = Nginx.Discover();

        Assert.That(Names(nginx.Config.SitesAvailable), Is.EquivalentTo([Consts.Files.DefaultSite, Consts.Files.LegacySite, Consts.Files.ReportsSite]),
            "SitesAvailable should hold every site of sites-available, enabled or not");
    }

    [Test]
    [Description("After Nginx.Discover() Config maps the Debian layout: Main, Default, ConfigDirectory and Snippets.")]
    public void MapsLayout()
    {
        var nginx = Nginx.Discover();

        Assert.Multiple(() =>
        {
            Assert.That(nginx.Config.Main.Path, Is.EqualTo(Consts.Files.MainPath), "Main should be nginx.conf of the config root");
            Assert.That(nginx.Config.Default.Path, Is.EqualTo(Path.Combine(Consts.Files.SitesAvailablePath, Consts.Files.DefaultSite)),
                "Default should be sites-available/default");
            Assert.That(Names(nginx.Config.ConfigDirectory), Is.EquivalentTo([Consts.Files.Upstreams]), "ConfigDirectory should hold the files of conf.d");
            Assert.That(Names(nginx.Config.Snippets), Does.Contain(Consts.Files.ProxyHeaders), "Snippets should hold the files of snippets");
        });
    }

    [Test]
    [Description("Nginx.Discover() reads which sites are enabled from the links in sites-enabled.")]
    public void ReadsEnabledSites()
    {
        var nginx = Nginx.Discover();

        Assert.Multiple(() =>
        {
            Assert.That(Names(nginx.SitesEnabled), Is.EquivalentTo([Consts.Files.DefaultSite, Consts.Files.ReportsSite]),
                "SitesEnabled should hold the sites linked in sites-enabled");
            Assert.That(nginx.SitesEnabled.Contains(nginx.Config.SitesAvailable[Consts.Files.LegacySite]), Is.False,
                "The site without a link should not be enabled");
        });
    }

    [Test]
    [Description("A site reached through its link in sites-enabled is the same object as its file in sites-available, loaded once.")]
    public void LinksAreOneFile()
    {
        var nginx = Nginx.Discover();

        Assert.Multiple(() =>
        {
            Assert.That(nginx.Config[Consts.Files.ReportsLink], Is.SameAs(nginx.Config.SitesAvailable[Consts.Files.ReportsSite]),
                "The link and the site should be one object");
            Assert.That(nginx.Config.All.Select(file => file.Path), Is.Unique, "Every file should be loaded once");
        });
    }

    [Test]
    [Description("Nginx.Discover() with another config root finds the conf.d layout of the official nginx image: what nginx loads, no sites-available.")]
    public async Task LoadsConfDLayout()
    {
        var root = Arrange.ConfDOnlyLayout();
        var loadedByNginx = await NginxDump.LoadedFilesAsync(Path.Combine(root, Consts.Files.OfficialMain));

        var nginx = Nginx.Discover(new NginxEnvironment(ConfigRoot: root));

        Assert.Multiple(() =>
        {
            Assert.That(loadedByNginx, Is.Not.Empty, "nginx -T should list the files of the official layout");
            Assert.That(nginx.Config.All.Select(file => file.Path), Is.SupersetOf(loadedByNginx), "Discover should load every file nginx -T lists");
            Assert.That(Names(nginx.Config.ConfigDirectory), Is.EquivalentTo([Consts.Files.OfficialDefault]), "ConfigDirectory should hold conf.d/default.conf");
            Assert.That(nginx.Config.SitesAvailable, Is.Empty, "A layout without sites-available should have no sites");
            Assert.That(() => nginx.Config.Default, Throws.TypeOf<KeyNotFoundException>(), "Default should not exist without sites-available");
        });
    }
}
