namespace Lakasabasz.Nginx.Sdk.Tests.Files;

[NginxRequired(Mode.Modify)]
public sealed class DiscoverChangesTests : TestTemplate
{
    [Test]
    [Description("A disabled site including a missing file does not fail Nginx.Discover(): includes are followed only from what nginx loads, and nginx -t passes.")]
    public async Task SkipsIncludesOfDisabledSites()
    {
        Arrange.DisabledSiteWithMissingInclude();

        var nginx = Nginx.Discover(sudo: true);
        var nginxTest = await NginxControl.TestAsync();

        Assert.Multiple(() =>
        {
            Assert.That(nginx.Config.SitesAvailable.TryGet(Consts.Files.OrphanName, out _), Is.True, "The disabled site should be loaded");
            Assert.That(nginxTest.Success, Is.True, "nginx should accept the config, as the disabled site is not loaded by it");
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
    public async Task ReportsDanglingLinks()
    {
        var ghost = Arrange.DanglingEnabledSite();

        var exception = Assert.Throws<NginxParseException>(() => Nginx.Discover(), "Discover should fail on the link to a missing site");
        var nginxTest = await NginxControl.TestAsync();

        Assert.Multiple(() =>
        {
            Assert.That(exception.Errors.Select(error => error.Message), Has.One.Contains(ghost), "The error should name the dangling link");
            Assert.That(nginxTest.Success, Is.False, "nginx -t should fail on the dangling link too");
            Assert.That(() => Nginx.Init(), Throws.Nothing, "Nginx.Init() should not fail on the dangling link");
        });
    }

    [Test]
    [Description("A regular file in sites-enabled is config, not a link: Nginx.Discover() loads it, SitesEnabled does not list it.")]
    public void LeavesPlainEnabledFilesToConfig()
    {
        Arrange.PlainEnabledSite();
        var nginx = Nginx.Discover();
        var plain = nginx.Config[Consts.Files.PlainEnabledSite];

        nginx.SitesEnabled.Disable(plain);

        Assert.Multiple(() =>
        {
            Assert.That(nginx.Config.All, Does.Contain(plain), "The file in sites-enabled should stay loaded as config");
            Assert.That(nginx.SitesEnabled, Does.Not.Contain(plain.Path), "SitesEnabled should list only links");
        });
    }
}
