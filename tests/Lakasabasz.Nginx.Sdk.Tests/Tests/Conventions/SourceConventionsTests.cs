namespace Lakasabasz.Nginx.Sdk.Tests.Conventions;

public sealed class SourceConventionsTests : TestTemplate
{
    private static readonly string[] Files = SourceScanner.ProjectFiles()
        .Where(file => Path.GetFileName(file) != Consts.Source.ConstsFile)
        .ToArray();

    [Test]
    [Description("No source file of the test project except Consts.cs contains a string or char literal.")]
    public void NoTextLiterals()
    {
        var literals = Files.SelectMany(SourceScanner.DisallowedLiterals)
            .Select(literal => Consts.Messages.Violation(literal.File, literal.Line, literal.Text));

        Assert.That(literals, Is.Empty, "Text literals outside Consts.cs");
    }

    [Test]
    [Description("Every Assert.That, Assume.That and Assert.Throws in the test project has a message saying what it checks.")]
    public void AssertsHaveMessages()
    {
        var withoutMessage = Files.SelectMany(SourceScanner.AssertCalls)
            .Where(call => call.Arguments < (call.Method == Consts.Source.AssertThrows ? 2 : 3))
            .Select(call => Consts.Messages.Violation(call.File, call.Line, call.Method));

        Assert.That(withoutMessage, Is.Empty, "Asserts without a message");
    }
}
