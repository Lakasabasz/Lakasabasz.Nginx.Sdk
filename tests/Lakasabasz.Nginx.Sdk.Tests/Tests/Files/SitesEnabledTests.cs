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
    [Description("SitesEnabled.Add() enables a disabled site, without making a link on disk.")]
    public void AddEnablesSite()
    {
        var legacy = _nginx.Config.SitesAvailable[Consts.Files.LegacySite];

        _nginx.SitesEnabled.Add(legacy);

        Assert.Multiple(() =>
        {
            Assert.That(_nginx.SitesEnabled.Contains(legacy), Is.True, "The site should be enabled");
            Assert.That(_nginx.SitesEnabled, Does.Contain(legacy), "The site should be among the enabled sites");
            Assert.That(File.Exists(Consts.Files.LegacyLinkPath), Is.False, "Add should not make a link on disk");
        });
    }

    [Test]
    [Description("SitesEnabled.Add() of an enabled site changes nothing.")]
    public void AddKeepsEnabledSite()
    {
        var reports = _nginx.Config.SitesAvailable[Consts.Files.ReportsSite];

        _nginx.SitesEnabled.Add(reports);

        Assert.That(_nginx.SitesEnabled.Count(site => site == reports), Is.EqualTo(1), "The site should be enabled once");
    }

    [Test]
    [Description("SitesEnabled.Remove() disables an enabled site.")]
    public void RemoveDisablesSite()
    {
        var reports = _nginx.Config.SitesAvailable[Consts.Files.ReportsSite];

        _nginx.SitesEnabled.Remove(reports);

        Assert.Multiple(() =>
        {
            Assert.That(_nginx.SitesEnabled.Contains(reports), Is.False, "The site should be disabled");
            Assert.That(_nginx.SitesEnabled, Does.Not.Contain(reports), "The site should leave the enabled sites");
        });
    }

    [Test]
    [Description("SitesEnabled.Add() refuses a file outside sites-available and a file of another Nginx object.")]
    public void AddRefusesOtherFiles()
    {
        var other = Nginx.Discover().Config.SitesAvailable[Consts.Files.LegacySite];

        Assert.Multiple(() =>
        {
            Assert.That(() => _nginx.SitesEnabled.Add(_nginx.Config.Main), Throws.ArgumentException, "nginx.conf should not be enabled as a site");
            Assert.That(() => _nginx.SitesEnabled.Add(other), Throws.ArgumentException, "A site of another Nginx object should be refused");
        });
    }

    [Test]
    [Description("A site created in sites-available can be enabled.")]
    public void EnablesCreatedSite()
    {
        var shop = _nginx.Config.SitesAvailable.Create(Consts.Files.NewSite);

        _nginx.SitesEnabled.Add(shop);

        Assert.That(_nginx.SitesEnabled.Contains(shop), Is.True, "The created site should be enabled");
    }
}
