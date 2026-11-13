namespace Lakasabasz.Nginx.Sdk.Tests.Config;

public sealed class ParseErrorTests : TestTemplate
{
    [Test]
    [Description("Loading a config that includes several broken files and a missing one throws one NginxParseException listing every error with its file, line and column.")]
    public void ListsEveryBrokenFile()
    {
        var main = Arrange.BrokenIncludes();

        var exception = Assert.Throws<NginxParseException>(() => Nginx.Load(main, followIncludes: true),
            "Loading broken files should throw NginxParseException");

        Assert.That(exception.Errors, Is.EqualTo(Consts.ParseErrors.BrokenIncludes(Path.GetDirectoryName(main)!)),
            "The exception should list the error of every broken file and the missing include, in loading order");
    }

    [Test]
    [Description("Without followIncludes, loading a config does not open the files it includes, so their errors do not matter.")]
    public void IgnoresIncludesWhenNotFollowed()
    {
        var main = Arrange.BrokenIncludes();

        var nginx = Nginx.Load(main);

        Assert.That(nginx.Config.All.Select(file => file.Path), Is.EqualTo<string[]>([main]), "Only the given file should be loaded");
    }
}
