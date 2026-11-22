namespace Lakasabasz.Nginx.Sdk.Tests.Server;

[NginxRequired(Mode.ReadOnly)]
public sealed class NginxServerTests : TestTemplate
{
    [Test]
    [Description("Nginx.Init() points at the standard installation: /usr/sbin/nginx and /etc/nginx.")]
    public void UsesInstalledNginx()
    {
        var nginx = Nginx.Init();

        Assert.Multiple(() =>
        {
            Assert.That(nginx.Server.Environment.Binary, Is.EqualTo(new NginxPath(Consts.Paths.Binary)), "Nginx.Init() should use the standard nginx binary");
            Assert.That(nginx.Server.Environment.ConfigRoot, Is.EqualTo(new NginxPath(Consts.Paths.ConfigRoot)), "Nginx.Init() should use the standard config root");
        });
    }
}
