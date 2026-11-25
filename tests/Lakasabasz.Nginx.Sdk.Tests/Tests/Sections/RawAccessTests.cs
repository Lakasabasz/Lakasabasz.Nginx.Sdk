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

    [Test]
    [Description("Typed changes of a server leave a directive of a module outside nginx as it was written, byte for byte.")]
    public void TypedChangesKeepCustom()
    {
        Server.Names.Add(Consts.Sections.AddedName);
        Server.Root = Consts.Sections.WebRoot;
        Server.Listens.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(Custom.Arguments, Is.EqualTo(Consts.Sections.CustomArguments), "The custom directive should keep its arguments");
            Assert.That(_file.Serialize(), Is.EqualTo(Expected.CustomKept), "Only the typed directives should change");
        });
    }
}
