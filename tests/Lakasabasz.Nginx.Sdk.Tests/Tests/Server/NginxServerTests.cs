namespace Lakasabasz.Nginx.Sdk.Tests.Server;

[NginxRequired(Mode.ReadOnly)]
public sealed class NginxServerTests : TestTemplate
{
    [Test]
    [Description("Server.Version of Nginx.Init() is the version that the installed nginx reports with nginx -v.")]
    public async Task ReadsVersion()
    {
        var nginx = Nginx.Init();

        var reported = await NginxProcess.RunAsync(Consts.Paths.Binary, Consts.Arguments.Version);
        Cancellation.ThrowIfCancellationRequested();

        Assert.That(reported.StandardError, Does.Contain(Consts.NginxOutput.Version(nginx.Server.Version)),
            "Server.Version should be the version that nginx -v prints");
    }

    [Test]
    [Description("Nginx.Init() points at the standard installation: /usr/sbin/nginx and /etc/nginx.")]
    public void UsesInstalledNginx()
    {
        var nginx = Nginx.Init();

        Assert.Multiple(() =>
        {
            Assert.That(nginx.Server.Environment.Binary, Is.EqualTo(Consts.Paths.Binary), "Nginx.Init() should use the standard nginx binary");
            Assert.That(nginx.Server.Environment.ConfigRoot, Is.EqualTo(Consts.Paths.ConfigRoot), "Nginx.Init() should use the standard config root");
        });
    }

    [Test]
    [Description("Server.Test() on the unchanged preset config succeeds and reports no errors.")]
    public void TestPassesValidConfig()
    {
        var nginx = Nginx.Init(sudo: true);

        var result = nginx.Server.Test();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, "Server.Test() should succeed on the unchanged preset config");
            Assert.That(result.Errors, Is.Empty, "Server.Test() should report no errors for the unchanged preset config");
        });
    }
}
