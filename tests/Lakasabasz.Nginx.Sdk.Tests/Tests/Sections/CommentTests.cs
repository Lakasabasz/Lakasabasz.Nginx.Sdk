namespace Lakasabasz.Nginx.Sdk.Tests.Sections;

using Server = Lakasabasz.Nginx.Sdk.Server;

[NginxRequired(Mode.ReadOnly)]
public sealed class CommentTests : TestTemplate
{
    private NginxFile _file;

    #region SetUp

    [SetUp]
    public void LoadMixed() => _file = NginxFile.Load(Arrange.MixedConfig());

    #endregion

    #region Helpers

    private Server Server => _file.Servers.Single();

    private Directive Listen => Server.Directives.Single(d => d.Name == Consts.Sections.Listen);

    private Directive ServerName => Server.Directives.Single(d => d.Name == Consts.Sections.ServerName);

    #endregion

    [Test]
    [Description("The comment at the end of a directive's line is read without # and the space after it.")]
    public void ReadsTrailingComment()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ServerName.Comment, Is.EqualTo(Consts.Sections.MixedComment), "The comment after server_name should be read");
            Assert.That(Listen.Comment, Is.Null, "listen has no comment");
        });
    }

    [Test]
    [Description("Comments are added at the end of a directive's line and of a block's opening line, changed and removed; nginx accepts the file.")]
    public async Task ChangesTrailingComments()
    {
        Listen.Comment = Consts.Sections.ListenComment;
        ServerName.Comment = null;
        Server.Locations.Last().Comment = Consts.Sections.LocationComment;
        Server.Locations.Add(Consts.Sections.ApiPath).Comment = Consts.Sections.ApiComment;

        var result = await NginxControl.TestAsync(_file, WorkDirectory);

        Assert.Multiple(() =>
        {
            Assert.That(_file.Serialize(), Is.EqualTo(Expected.MixedCommented), "Only the commented lines should change");
            Assert.That(result.Success, Is.True, Consts.Messages.NginxRejected(result));
        });
    }

    [Test]
    [Description("AddComment writes a comment on a line of its own after the last directive, also in a block written on one line.")]
    public async Task AddsCommentLines()
    {
        Server.Directives.AddComment(Consts.Sections.LineComment);
        Server.Locations.First().Directives.AddComment(Consts.Sections.LineComment);

        var result = await NginxControl.TestAsync(_file, WorkDirectory);

        Assert.Multiple(() =>
        {
            Assert.That(_file.Serialize(), Is.EqualTo(Expected.MixedCommentLines), "Each comment should be on a line of its own");
            Assert.That(Server.Locations.First().Layout.OneLine, Is.False, "The block on one line should be on lines now");
            Assert.That(result.Success, Is.True, Consts.Messages.NginxRejected(result));
        });
    }

    [Test]
    [Description("A comment is one line: a line break in it is refused.")]
    public void LineBreakThrows()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => Listen.Comment = Consts.Sections.TwoLineComment, "A trailing comment with a line break should fail");
            Assert.Throws<ArgumentException>(() => Server.Directives.AddComment(Consts.Sections.TwoLineComment), "A comment line with a line break should fail");
        });
    }
}
