namespace Lakasabasz.Nginx.Sdk.Tests.Config;

public sealed class ParsingTests : TestTemplate
{
    #region Helpers

    private static Directive Named(DirectiveCollection directives, string name) => directives.First(d => d.Name == name);

    #endregion

    [Test]
    [Description("Loading a config reads the directives of every level and their arguments as nginx does: quotes removed, escapes resolved, variables and comments kept apart.")]
    public void ReadsArguments()
    {
        var file = Nginx.Load(Arrange.ArgumentsConfig()).Config.All.Single();
        var http = Named(file.Directives, Consts.Parsing.Http);
        var server = Named(http.Directives, Consts.Parsing.Server);
        var headers = server.Directives.Where(d => d.Name == Consts.Parsing.AddHeader).ToList();
        var locations = server.Directives.Where(d => d.Name == Consts.Parsing.Location).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(file.Directives.Select(d => d.Name),
                Is.EqualTo<string[]>([Consts.Parsing.WorkerProcesses, Consts.Parsing.Events, Consts.Parsing.Http]),
                "The top level should have worker_processes, events and http, without the comments");
            Assert.That(Named(file.Directives, Consts.Parsing.WorkerProcesses).Arguments, Is.EqualTo(Consts.Parsing.WorkerProcessesArguments),
                "Extra spaces and a comment after the directive should not be arguments");
            Assert.That(Named(http.Directives, Consts.Parsing.LogFormat).Arguments, Is.EqualTo(Consts.Parsing.LogFormatArguments),
                "A single-quoted argument should keep double quotes and variables inside it");
            Assert.That(Named(server.Directives, Consts.Parsing.ServerName).Arguments, Is.EqualTo(Consts.Parsing.ServerNameArguments),
                "Single- and double-quoted arguments should lose their quotes and keep their spaces");
            Assert.That(Named(server.Directives, Consts.Parsing.Set).Arguments, Is.EqualTo(Consts.Parsing.SetArguments),
                "${host} in an unquoted argument should be a variable, not the start of a block");
            Assert.That(headers[0].Arguments, Is.EqualTo(Consts.Parsing.CharsHeaderArguments),
                "Inside quotes ; { } # and spaces should be part of the argument");
            Assert.That(headers[1].Arguments, Is.EqualTo(Consts.Parsing.EscapesHeaderArguments),
                "\\\" \\\\ and \\t should be unescaped and an unknown escape should keep its backslash, as nginx does");
            Assert.That(locations[0].Arguments, Is.EqualTo(Consts.Parsing.RegexLocationArguments),
                "A regex argument should keep its backslash and $");
            Assert.That(Named(locations[1].Directives, Consts.Parsing.Return).Arguments, Is.EqualTo(Consts.Parsing.BracesReturnArguments),
                "} inside an unquoted argument should be part of it, not the end of the block");
        });
    }

    [Test]
    [Description("A loaded directive knows the line and column where it starts in the file.")]
    public void KeepsPositions()
    {
        var file = Nginx.Load(Arrange.ArgumentsConfig()).Config.All.Single();
        var workerProcesses = Named(file.Directives, Consts.Parsing.WorkerProcesses);
        var header = Named(Named(Named(file.Directives, Consts.Parsing.Http).Directives, Consts.Parsing.Server).Directives, Consts.Parsing.AddHeader);

        Assert.Multiple(() =>
        {
            Assert.That(workerProcesses.Line, Is.EqualTo(Consts.Parsing.WorkerProcessesLine), "worker_processes should be on the line after the comment");
            Assert.That(workerProcesses.Column, Is.EqualTo(1), "worker_processes should start the line");
            Assert.That(header.Line, Is.EqualTo(Consts.Parsing.CharsHeaderLine), "The first add_header should be on its line in the nested blocks");
            Assert.That(header.Column, Is.EqualTo(Consts.Parsing.CharsHeaderColumn), "The first add_header should start after two tabs");
        });
    }

    [Test]
    [Description("A Lua block is kept as opaque text, braces in its strings and comments included, and the directives after it are read normally.")]
    public void KeepsRawBlocks()
    {
        var path = Arrange.LuaConfig();
        var file = Nginx.Load(path).Config.All.Single();
        var location = Named(file.Directives, Consts.Parsing.Location);
        var lua = Named(location.Directives, Consts.Parsing.ContentByLua);

        Assert.Multiple(() =>
        {
            Assert.That(lua.IsBlock, Is.True, "The Lua block should be a block");
            Assert.That(lua.RawContent, Is.EqualTo(Consts.Parsing.LuaBody), "The Lua block should keep its code exactly, up to its own closing brace");
            Assert.That(lua.Directives, Is.Empty, "The Lua code should not be read as directives");
            Assert.That(Named(location.Directives, Consts.Parsing.Return).Arguments, Is.EqualTo(Consts.Parsing.LuaReturnArguments),
                "The directive after the Lua block should be read normally");
            Assert.That(file.Serialize(), Is.EqualTo(File.ReadAllText(path)), "The file with the Lua block should be serialized as loaded");
        });
    }

    [Test]
    [Description("CRLF line endings and a missing final line break survive loading and serializing.")]
    public void KeepsLineEndings()
    {
        var crlf = Arrange.CrlfConfig();
        var noFinalNewline = Arrange.NoFinalNewlineConfig();

        Assert.Multiple(() =>
        {
            Assert.That(Nginx.Load(crlf).Config.All.Single().Serialize(), Is.EqualTo(File.ReadAllText(crlf)),
                "A CRLF file should be serialized with its CRLF line endings");
            Assert.That(Nginx.Load(noFinalNewline).Config.All.Single().Serialize(), Is.EqualTo(File.ReadAllText(noFinalNewline)),
                "A file without a final line break should not get one");
        });
    }
}
