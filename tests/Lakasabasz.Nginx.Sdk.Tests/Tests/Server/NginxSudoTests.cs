namespace Lakasabasz.Nginx.Sdk.Tests.Server;

[NginxRequired(Mode.ReadOnly)]
public sealed class NginxSudoTests : TestTemplate
{
    [Test]
    [Description("With sudo: true, Server.Test() runs nginx as root through sudo -n and succeeds.")]
    public void WorksThroughSudo()
    {
        var nginx = Nginx.Init(sudo: true);

        Assert.That(nginx.Server.Test().Success, Is.True, "Server.Test() through sudo should succeed");
    }

    [Test]
    [Description("Without sudo, nginx run by an unprivileged user cannot open its pid file: Server.Test() fails and reports the error instead of throwing.")]
    public void FailsWithoutSudo()
    {
        var nginx = Nginx.Init(sudo: false);

        var result = nginx.Server.Test();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, "Without sudo, Server.Test() should fail: an unprivileged nginx cannot open its pid file");
            Assert.That(result.Errors, Is.EqualTo(Consts.NginxLogs.PidFilePermissionDeniedLogs),
                "Without sudo, Server.Test() should report the permission error on the pid file");
        });
    }

    [Test]
    [Description("A binary path with spaces that sudo allows reaches sudo and nginx intact: Server.Test() and Version work.")]
    public void SudoWithSpaces()
    {
        var nginx = Nginx.Init(new NginxEnvironment(Binary: Consts.Paths.BinaryWithSpaces, Sudo: true));

        Assert.Multiple(() =>
        {
            Assert.That(nginx.Server.Test().Success, Is.True, "Server.Test() through sudo should succeed for a binary path with spaces");
            Assert.That(nginx.Server.Version, Is.EqualTo(Nginx.Init().Server.Version), "Server.Version through a binary path with spaces should be the installed nginx version");
        });
    }

    [Test]
    [Description("A binary that sudo does not allow without a password: Server.Test() throws NginxCommandException instead of waiting for a password.")]
    public void RefusesUnlistedBinary()
    {
        var nginx = Nginx.Init(new NginxEnvironment(Binary: Arrange.UnlistedBinary(), Sudo: true));

        var exception = Assert.Throws<NginxCommandException>(() => nginx.Server.Test(), "Server.Test() should throw NginxCommandException when sudo does not allow the binary");

        Assert.That(exception.Message, Does.Contain(Consts.NginxOutput.SudoPasswordRequired), "The exception should say that sudo asked for a password instead of running nginx");
    }
}
