namespace Lakasabasz.Nginx.Sdk.Tests.Infrastructure;

public sealed class TestArrange(string configRoot, Func<string> workDirectory)
{
    public string BrokenConfig() => WriteConfig(Consts.Config.BrokenFile, Consts.Config.BrokenContent);

    public string DuplicateServer() => WriteConfig(Consts.Config.DuplicateServerFile, Consts.Config.DuplicateServerContent);

    public string ChangedReportsHealth()
    {
        var site = ConfigPath(Consts.Config.ReportsSite);
        File.WriteAllText(site, File.ReadAllText(site)
            .Replace(Consts.Config.ReportsHealthReturn, Consts.Config.ReloadedHealthReturn, StringComparison.Ordinal));
        return site;
    }

    public string MissingBinary() => Path.Combine(workDirectory(), Consts.Paths.MissingBinary);

    public string UnlistedBinary()
    {
        var binary = Path.Combine(workDirectory(), Consts.Paths.UnlistedBinary);
        File.CreateSymbolicLink(binary, Consts.Paths.Binary);
        return binary;
    }

    public string ArgumentsConfig() => Copy(Consts.Fixtures.Arguments);

    public string LuaConfig() => Copy(Consts.Fixtures.Lua);

    public string CrlfConfig() => Copy(Consts.Fixtures.Crlf);

    public string NoFinalNewlineConfig() => Copy(Consts.Fixtures.NoFinalNewline);

    public string SiteConfig() => Copy(Consts.Fixtures.Site);

    public string InlineConfig() => Copy(Consts.Fixtures.Inline);

    public IReadOnlyList<string> BrokenConfigs() => Directory
        .EnumerateFiles(Fixture.PathOf(Consts.Fixtures.Errors))
        .Order(StringComparer.Ordinal)
        .Select(file => Copy(Path.Combine(Consts.Fixtures.Errors, Path.GetFileName(file))))
        .ToList();

    public string BrokenIncludes()
    {
        var directory = workDirectory();
        var broken = Directory.CreateDirectory(Path.Combine(directory, Consts.Fixtures.BrokenDirectory)).FullName;
        foreach (var file in Directory.EnumerateFiles(Fixture.PathOf(Consts.Fixtures.Errors)))
            File.Copy(file, Path.Combine(broken, Path.GetFileName(file)));
        var main = Path.Combine(directory, Consts.Fixtures.BrokenMain);
        File.WriteAllText(main, Consts.Fixtures.BrokenMainContent(directory));
        return main;
    }

    public string ValuesSite()
    {
        var target = ConfigPath(Consts.Fixtures.ValuesTarget);
        File.Copy(Fixture.PathOf(Consts.Fixtures.Values), target);
        return target;
    }

    private string Copy(string fixture)
    {
        var target = Path.Combine(workDirectory(), Path.GetFileName(fixture));
        File.Copy(Fixture.PathOf(fixture), target);
        return target;
    }

    private string WriteConfig(string relativePath, string content)
    {
        var path = ConfigPath(relativePath);
        File.WriteAllText(path, content);
        return path;
    }

    private string ConfigPath(string relativePath) => Path.Combine(configRoot, relativePath);
}
