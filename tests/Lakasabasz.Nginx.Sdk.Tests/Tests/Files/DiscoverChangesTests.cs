namespace Lakasabasz.Nginx.Sdk.Tests.Files;

[NginxRequired(Mode.Modify)]
public sealed class DiscoverChangesTests : TestTemplate
{
    [Test]
    [Description("A disabled site including a missing file does not fail Nginx.Discover(): includes are followed only from what nginx loads, and nginx -t passes.")]
    public void SkipsIncludesOfDisabledSites()
    {
        Arrange.DisabledSiteWithMissingInclude();

        var nginx = Nginx.Discover(sudo: true);

        Assert.Multiple(() =>
        {
            Assert.That(nginx.Config.SitesAvailable.TryGet(Consts.Files.OrphanName, out _), Is.True, "The disabled site should be loaded");
            Assert.That(nginx.Server.Test().Success, Is.True, "nginx should accept the config, as the disabled site is not loaded by it");
        });
    }

    [Test]
    [Description("Syntax errors in a disabled site and a snippet fail Nginx.Discover() with one NginxParseException listing both files.")]
    public void ListsEveryBrokenFile()
    {
        var broken = Arrange.BrokenSiteAndSnippet();

        var exception = Assert.Throws<NginxParseException>(() => Nginx.Discover(), "Discover should fail on the broken files");

        Assert.That(exception.Errors.Select(error => error.File), Is.EquivalentTo(broken), "The exception should list both broken files");
    }

    [Test]
    [Description("A link in sites-enabled to a missing site fails Nginx.Discover() as it fails nginx -t, while Nginx.Init() still works.")]
    public void ReportsDanglingLinks()
    {
        var ghost = Arrange.DanglingEnabledSite();

        var exception = Assert.Throws<NginxParseException>(() => Nginx.Discover(), "Discover should fail on the link to a missing site");

        Assert.Multiple(() =>
        {
            Assert.That(exception.Errors.Select(error => error.Message), Has.One.Contains(ghost), "The error should name the dangling link");
            Assert.That(Nginx.Init(sudo: true).Server.Test().Success, Is.False, "nginx -t should fail on the dangling link too");
            Assert.That(() => Nginx.Init(), Throws.Nothing, "Nginx.Init() should not fail on the dangling link");
        });
    }

    [Test]
    [Description("A regular file in sites-enabled is an enabled site that SitesEnabled.Remove() refuses: there is no link to remove.")]
    public void KeepsPlainEnabledFiles()
    {
        Arrange.PlainEnabledSite();
        var nginx = Nginx.Discover();
        var plain = nginx.Config[Consts.Files.PlainEnabledSite];

        Assert.Multiple(() =>
        {
            Assert.That(nginx.SitesEnabled.Contains(plain), Is.True, "The file in sites-enabled should be enabled");
            Assert.That(() => nginx.SitesEnabled.Remove(plain), Throws.InvalidOperationException, "Remove should refuse a file that is not a link");
        });
    }
}
