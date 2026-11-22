namespace Lakasabasz.Nginx.Sdk.Tests.Server;

[NginxRequired(Mode.Modify)]
public sealed class NginxServerChangeTests : TestTemplate
{
    [Test]
    [Description("After adding a config file with an unknown directive, Server.Test() fails without throwing and reports the error with its file and line.")]
    public void TestReportsErrorLocation()
    {
        var nginx = Nginx.Init(sudo: true);
        Arrange.BrokenConfig();

        var result = nginx.Server.Test();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, "Server.Test() should report failure for a config with an unknown directive");
            Assert.That(result.Errors, Is.EqualTo(Consts.NginxLogs.UnknownDirectiveLogs),
                "Server.Test() should report only the unknown directive, with the file and line of the broken config");
        });
    }

    [Test]
    [Description("After adding a server with a conflicting server_name, Server.Test() succeeds and reports the nginx warning in Warnings, not in Errors.")]
    public void TestReportsWarnings()
    {
        var nginx = Nginx.Init(sudo: true);
        Arrange.DuplicateServer();

        var result = nginx.Server.Test();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, "A conflicting server_name is only a warning: Server.Test() should succeed");
            Assert.That(result.Errors, Is.Empty, "A conflicting server_name should not be reported as an error");
            Assert.That(result.Warnings, Is.EqualTo(Consts.NginxLogs.ConflictingServerNameLogs),
                "Server.Test() should report the conflicting server_name warning");
        });
    }

    [Test]
    [Description("After changing a site on disk, Server.Reload() makes the running nginx serve the new content.")]
    public async Task ReloadAppliesConfig()
    {
        var nginx = Nginx.Init(sudo: true);
        Arrange.ChangedReportsHealth();

        nginx.Server.Reload();

        await NginxHttp.EventuallyAsync(
            async () => await NginxHttp.GetHealthAsync(Cancellation) == Consts.Http.ReloadedHealth,
            "After Server.Reload() nginx should serve the changed /health response",
            Cancellation);
    }
}
