namespace Lakasabasz.Nginx.Sdk.Tests.Files;

[NginxRequired(Mode.ReadOnly)]
public sealed class FileTests : TestTemplate
{
    private Nginx _nginx;

    #region SetUp

    [SetUp]
    public void DiscoverNginx() => _nginx = Nginx.Discover();

    #endregion

    [Test]
    [Description("SitesAvailable.Create() adds an empty file to the directory and to Config.All, without writing it.")]
    public void CreateAddsFile()
    {
        var shop = _nginx.Config.SitesAvailable.Create(Consts.Files.NewSite);

        Assert.Multiple(() =>
        {
            Assert.That(shop.Path, Is.EqualTo(Path.Combine(Consts.Files.SitesAvailablePath, Consts.Files.NewSite)), "The new site should be in sites-available");
            Assert.That(_nginx.Config.All, Does.Contain(shop), "The new site should be in Config.All");
            Assert.That(_nginx.Config.SitesAvailable[Consts.Files.NewSite], Is.SameAs(shop), "The indexer should return the new site");
            Assert.That(shop.Serialize(), Is.Empty, "The new site should be empty");
            Assert.That(File.Exists(shop.Path), Is.False, "Create should not write the file");
        });
    }

    [Test]
    [Description("A created file takes directives like a loaded one.")]
    public void CreatedFileIsEditable()
    {
        var shop = _nginx.Config.SitesAvailable.Create(Consts.Files.NewSite);

        shop.Directives.AddBlock(Consts.Files.Server).Directives.Add(Consts.Files.Listen, Consts.Files.ShopPort);

        Assert.That(shop.Serialize(), Is.EqualTo(Consts.Files.ShopContent), "The new site should hold the server block with its listen");
    }

    [Test]
    [Description("Create() refuses a file that is loaded already.")]
    public void CreateRefusesLoadedFile()
    {
        Assert.That(() => _nginx.Config.SitesAvailable.Create(Consts.Files.ReportsSite), Throws.InvalidOperationException,
            "Create should refuse the loaded site reports");
    }

    [Test]
    [Description("Create() refuses a file that exists on disk, even when it is not loaded.")]
    public void CreateRefusesFileOnDisk()
    {
        var nginx = Nginx.Init();

        Assert.That(() => nginx.Config.SitesAvailable.Create(Consts.Files.ReportsSite), Throws.InvalidOperationException,
            "Create should refuse the site reports that exists on disk");
    }

    [Test]
    [Description("Create() takes a file name only, not a path.")]
    public void CreateRefusesPaths()
    {
        Assert.That(() => _nginx.Config.SitesAvailable.Create(Consts.Files.NotAFileName), Throws.ArgumentException,
            "Create should refuse a name with a directory");
    }

    [Test]
    [Description("The indexer of a directory throws KeyNotFoundException for a file that is not loaded; there is no get-or-create.")]
    public void IndexerThrowsWhenMissing()
    {
        Assert.That(() => _nginx.Config.SitesAvailable[Consts.Files.MissingSite], Throws.TypeOf<KeyNotFoundException>(),
            "The indexer should throw for a missing site");
    }

    [Test]
    [Description("TryGet() returns a loaded file and reports a missing one with false.")]
    public void TryGetReportsMissing()
    {
        var found = _nginx.Config.SitesAvailable.TryGet(Consts.Files.ReportsSite, out var reports);
        var missing = _nginx.Config.SitesAvailable.TryGet(Consts.Files.MissingSite, out var none);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True, "TryGet should find the site reports");
            Assert.That(reports, Is.SameAs(_nginx.Config.SitesAvailable[Consts.Files.ReportsSite]), "TryGet should return the loaded site");
            Assert.That(missing, Is.False, "TryGet should not find a missing site");
            Assert.That(none, Is.Null, "TryGet should return null for a missing site");
        });
    }

    [Test]
    [Description("Delete() removes the file from its directory and Config.All and disables it, without touching the disk.")]
    public void DeleteRemovesFile()
    {
        var reports = _nginx.Config.SitesAvailable[Consts.Files.ReportsSite];

        _nginx.Config.SitesAvailable.Delete(Consts.Files.ReportsSite);

        Assert.Multiple(() =>
        {
            Assert.That(_nginx.Config.All, Does.Not.Contain(reports), "The deleted site should leave Config.All");
            Assert.That(_nginx.Config.SitesAvailable.TryGet(Consts.Files.ReportsSite, out _), Is.False, "The deleted site should leave SitesAvailable");
            Assert.That(_nginx.SitesEnabled.Contains(reports), Is.False, "The deleted site should be disabled");
            Assert.That(File.Exists(reports.Path), Is.True, "Delete should not remove the file from disk");
        });
    }

    [Test]
    [Description("Delete() of a file that is not loaded throws KeyNotFoundException.")]
    public void DeleteThrowsWhenMissing()
    {
        Assert.That(() => _nginx.Config.SitesAvailable.Delete(Consts.Files.MissingSite), Throws.TypeOf<KeyNotFoundException>(),
            "Delete should throw for a missing site");
    }

    [Test]
    [Description("After Nginx.Init() Config.All is empty; Config.Load() adds one file, and loading it again returns the same object.")]
    public void LoadAddsFile()
    {
        var nginx = Nginx.Init();
        var before = nginx.Config.All.ToList();

        var legacy = nginx.Config.Load(Consts.Files.LegacyRelativePath);

        Assert.Multiple(() =>
        {
            Assert.That(before, Is.Empty, "Nginx.Init() should load no files");
            Assert.That(nginx.Config.All, Is.EqualTo([legacy]), "Config.Load should add the file to Config.All");
            Assert.That(nginx.Config.Load(Consts.Files.LegacyRelativePath), Is.SameAs(legacy), "Loading the file again should return the same object");
        });
    }

    [Test]
    [Description("The Config indexer takes a path relative to the config root or an absolute one.")]
    public void IndexerTakesPaths()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_nginx.Config[Consts.Files.MainPath], Is.SameAs(_nginx.Config.Main), "The absolute path should give the main file");
            Assert.That(_nginx.Config[Consts.Files.ReportsRelativePath], Is.SameAs(_nginx.Config.SitesAvailable[Consts.Files.ReportsSite]),
                "The relative path should give the site");
        });
    }

    [Test]
    [Description("Config.Find() matches a file name, a glob in the file name and a glob in the path relative to the config root.")]
    public void FindMatchesNamesAndPaths()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_nginx.Config.Find(Consts.Files.Upstreams), Is.EqualTo(_nginx.Config.ConfigDirectory.ToList()), "The name should find conf.d/upstreams.conf");
            Assert.That(_nginx.Config.Find(Consts.Files.ConfigDirectoryFiles), Is.EquivalentTo(_nginx.Config.ConfigDirectory), "conf.d/* should find the files of conf.d");
            Assert.That(_nginx.Config.Find(Consts.Files.AllConfFiles).Select(file => file.Path), Has.All.EndsWith(Consts.Files.ConfExtension),
                "*.conf should find only .conf files");
            Assert.That(_nginx.Config.Find(Consts.Files.AllConfFiles), Does.Contain(_nginx.Config.Main), "*.conf should find nginx.conf");
        });
    }

    [Test]
    [Description("Config.Find() searches only loaded files, and finds enabled sites by their files in sites-available.")]
    public void FindSearchesLoadedFiles()
    {
        var nginx = Nginx.Init();
        var legacy = nginx.Config.Load(Consts.Files.LegacyRelativePath);

        Assert.Multiple(() =>
        {
            Assert.That(nginx.Config.Find(Consts.Files.SitesAvailableFiles), Is.EqualTo([legacy]), "Only the loaded site should be found");
            Assert.That(_nginx.Config.Find(Consts.Files.SitesEnabledFiles), Is.Empty, "Enabled sites should not be found by their links");
        });
    }
}
