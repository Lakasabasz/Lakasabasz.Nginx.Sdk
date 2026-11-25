namespace Lakasabasz.Nginx.Sdk.Tests.Sections;

using Server = Lakasabasz.Nginx.Sdk.Server;

[NginxRequired(Mode.ReadOnly)]
public sealed class CommentTests : TestTemplate
{
    private NginxFile _file;

    #region SetUp

    [SetUp]
    public void LoadComments() => _file = NginxFile.Load(Arrange.CommentsConfig());

    #endregion

    #region Helpers

    private Server Server => _file.Servers.Single();

    private Directive Listen => Server.Directives.Single(d => d.Name == Consts.Sections.Listen);

    private Directive ServerName => Server.Directives.Single(d => d.Name == Consts.Sections.ServerName);

    private Location Static => Server.Locations.Single(l => l.Path == Consts.Sections.StaticPath);

    private Location Api => Server.Locations.Single(l => l.Path == Consts.Sections.ApiPath);

    private Location Health => Server.Locations.Single(l => l.Path == Consts.Sections.HealthPath);

    #endregion

    [Test]
    [Description("Comments are read by fixed rules: at the end of a directive's line, on the lines right above a block, at the top of the file before an empty line.")]
    public void ReadsComments()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_file.Comment, Is.EqualTo(Consts.Sections.FileComment), "The lines at the top before an empty line should be the file's comment");
            Assert.That(Server.Comment, Is.EqualTo(Consts.Sections.ServerComment), "The line right above the server should be its comment");
            Assert.That(ServerName.Comment, Is.EqualTo(Consts.Sections.NameComment), "The comment after server_name should be read");
            Assert.That(Listen.Comment, Is.Null, "listen has no comment");
            Assert.That(Static.Comment, Is.EqualTo(Consts.Sections.StaticComment), "Both lines right above the location should be its comment, commented-out code too");
            Assert.That(Api.Comment, Is.Null, "A comment with an empty line before the block belongs to nothing");
            Assert.That(Health.Comment, Is.Null, "A comment after a block's closing brace is not its comment");
        });
    }

    [Test]
    [Description("Comments are set, changed and removed: at the end of a directive's line, above a block, at the top of the file; nginx accepts the file.")]
    public async Task ChangesComments()
    {
        Listen.Comment = Consts.Sections.ListenComment;
        ServerName.Comment = null;
        Static.Comment = Consts.Sections.LocationComment;
        Api.Comment = Consts.Sections.ApiComment;
        Server.Comment = null;
        _file.Comment = Consts.Sections.NewFileComment;

        var result = await NginxControl.TestAsync(_file, WorkDirectory);

        Assert.Multiple(() =>
        {
            Assert.That(_file.Serialize(), Is.EqualTo(Expected.CommentsChanged), "Only the commented lines should change");
            Assert.That(result.Success, Is.True, Consts.Messages.NginxRejected(result));
        });
    }

    [Test]
    [Description("A new block gets its comment right above it, under the spacing; a removed block takes its comment and the empty lines above along.")]
    public void CommentsMoveWithBlocks()
    {
        var added = Server.Locations.Add(Consts.Sections.NewPath);
        added.Comment = Consts.Sections.AddedComment;
        added.Root = Consts.Sections.WebRoot;
        Static.Remove();
        _file.Comment = null;

        Assert.That(_file.Serialize(), Is.EqualTo(Expected.CommentsMoved), "The new location should have its comment above it; the removed one should leave no comment and no spacing");
    }

    [Test]
    [Description("A comment on a directive of a block written on one line puts the block onto lines first, so the comment comments out nothing.")]
    public void OneLineUnfolds()
    {
        Health.Directives.Single().Comment = Consts.Sections.LineComment;

        Assert.Multiple(() =>
        {
            Assert.That(_file.Serialize(), Is.EqualTo(Expected.CommentsUnfolded), "The location should be written on lines with the comment after return");
            Assert.That(Health.Style.Braces, Is.EqualTo(BraceStyle.EndOfLine), "The location should no longer be on one line");
        });
    }

    [Test]
    [Description("Comment lines at the top of a file right above a block are the block's, not the file's; removing the only block keeps the file's comment and its empty line.")]
    public void TopCommentRules()
    {
        var mixed = NginxFile.Load(Arrange.MixedConfig());

        Server.Remove();

        Assert.Multiple(() =>
        {
            Assert.That(mixed.Comment, Is.Null, "A comment at the top with no empty line after it should not be the file's");
            Assert.That(mixed.Servers.Single().Comment, Is.EqualTo(Consts.Sections.MixedHeader), "It should be the comment of the block right under it");
            Assert.That(_file.Serialize(), Is.EqualTo(Consts.Sections.FileCommentOnly), "The file's comment and its empty line should stay");
        });
    }

    [Test]
    [Description("A directive's comment is one line; a block's or a file's has lines separated by \\n only.")]
    public void LineBreakThrows()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => Listen.Comment = Consts.Sections.TwoLineComment, "A directive's comment with a line break should fail");
            Assert.Throws<ArgumentException>(() => Static.Comment = Consts.Sections.CarriageReturnComment, "A block's comment with \\r should fail");
            Assert.Throws<ArgumentException>(() => _file.Comment = Consts.Sections.CarriageReturnComment, "A file's comment with \\r should fail");
        });
    }
}
