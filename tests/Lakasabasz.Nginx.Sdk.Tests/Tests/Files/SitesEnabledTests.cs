namespace Lakasabasz.Nginx.Sdk.Tests.Files;

[NginxRequired(Mode.ReadOnly)]
public sealed class SitesEnabledTests : TestTemplate
{
    private Nginx _nginx;

    #region SetUp

    [SetUp]
    public void DiscoverNginx() => _nginx = Nginx.Discover();

    #endregion

    [Test]
    [Description("SitesEnabled.Enable() enables a disabled site by its path, without making a link on disk.")]
    public void EnablesSite()
    {
        var legacy = _nginx.Config.SitesAvailable[Consts.Files.LegacySite];

        _nginx.SitesEnabled.Enable(legacy);

        Assert.Multiple(() =>
        {
            Assert.That(_nginx.SitesEnabled.Contains(legacy), Is.True, "The site should be enabled");
            Assert.That(_nginx.SitesEnabled, Does.Contain(legacy.Path), "The site's path should be among the enabled sites");
            Assert.That(File.Exists(Consts.Files.LegacyLinkPath), Is.False, "Enable should not make a link on disk");
        });
    }

    [Test]
    [Description("SitesEnabled.Enable() of an enabled site changes nothing.")]
    public void EnableKeepsEnabledSite()
    {
        var reports = _nginx.Config.SitesAvailable[Consts.Files.ReportsSite];

        _nginx.SitesEnabled.Enable(reports);

        Assert.That(_nginx.SitesEnabled.Count(site => site == reports.Path), Is.EqualTo(1), "The site should be enabled once");
    }

    [Test]
    [Description("SitesEnabled.Disable() disables an enabled site.")]
    public void DisablesSite()
    {
        var reports = _nginx.Config.SitesAvailable[Consts.Files.ReportsSite];

        _nginx.SitesEnabled.Disable(reports);

        Assert.Multiple(() =>
        {
            Assert.That(_nginx.SitesEnabled.Contains(reports), Is.False, "The site should be disabled");
            Assert.That(_nginx.SitesEnabled, Does.Not.Contain(reports.Path), "The site's path should leave the enabled sites");
        });
    }

    [Test]
    [Description("SitesEnabled.Enable() refuses a file outside sites-available.")]
    public void EnableRefusesOtherFiles() =>
        Assert.That(() => _nginx.SitesEnabled.Enable(_nginx.Config.Main), Throws.ArgumentException, "nginx.conf should not be enabled as a site");

    [Test]
    [Description("SitesEnabled uses only the site's path: a site loaded by another Nginx object is enabled, and nothing is added to the config.")]
    public void EnableUsesOnlyPath()
    {
        var other = Nginx.Discover().Config.SitesAvailable[Consts.Files.LegacySite];
        var loaded = _nginx.Config.All.Count;

        _nginx.SitesEnabled.Enable(other);

        Assert.Multiple(() =>
        {
            Assert.That(_nginx.SitesEnabled.Contains(_nginx.Config.SitesAvailable[Consts.Files.LegacySite]), Is.True,
                "The site with that path should be enabled");
            Assert.That(_nginx.Config.All, Has.Count.EqualTo(loaded), "Enabling should not add files to the config");
        });
    }

    [Test]
    [Description("A site created in sites-available can be enabled.")]
    public void EnablesCreatedSite()
    {
        var shop = _nginx.Config.SitesAvailable.Create(Consts.Files.NewSite);

        _nginx.SitesEnabled.Enable(shop);

        Assert.That(_nginx.SitesEnabled.Contains(shop), Is.True, "The created site should be enabled");
    }
}
