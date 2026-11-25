namespace Lakasabasz.Nginx.Sdk.Tests.Sections;

using Server = Lakasabasz.Nginx.Sdk.Server;

public sealed class RawAccessTests : TestTemplate
{
    private NginxFile _file;

    #region SetUp

    [SetUp]
    public void LoadCustom() => _file = NginxFile.Load(Arrange.CustomConfig());

    #endregion

    #region Helpers

    private Server Server => _file.Servers.Single();

    private Directive Custom => Server.Directives.Single(d => d.Name == Consts.Sections.CustomDirective);

    #endregion

    [Test]
    [Description("A directive of a module outside nginx is read raw: name, arguments and its text as written in the file.")]
    public void ReadsCustomDirective()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Custom.Arguments, Is.EqualTo(Consts.Sections.CustomArguments), "The arguments should be read without quotes");
            Assert.That(Custom.Text, Is.EqualTo(Consts.Sections.CustomText), "Its text should be as written, with its odd spacing");
            Assert.That(Server.Directive.Text, Is.EqualTo(Fixture.Read(Consts.Fixtures.Custom).TrimEnd()), "The server's text should be the whole block as written");
        });
    }
}
