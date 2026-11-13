namespace Lakasabasz.Nginx.Sdk.Tests.Server;

public sealed class NginxInitTests : TestTemplate
{
    [Test]
    [Description("Server.Test() on an installation without the nginx binary throws NginxCommandException without an exit code.")]
    public void MissingBinaryThrows()
    {
        var nginx = Nginx.Init(new NginxEnvironment(Binary: Arrange.MissingBinary()));

        var exception = Assert.Throws<NginxCommandException>(() => nginx.Server.Test(), "Server.Test() should throw NginxCommandException when the nginx binary does not exist");

        Assert.That(exception.ExitCode, Is.Null, "The command never ran, so NginxCommandException should have no exit code");
    }
}
