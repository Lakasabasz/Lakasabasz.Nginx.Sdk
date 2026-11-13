namespace Lakasabasz.Nginx.Sdk.Tests.Infrastructure;

public static class Fixture
{
    public static string PathOf(string relativePath) => Path.Combine(AppContext.BaseDirectory, Consts.Fixtures.Root, relativePath);

    public static string Read(string relativePath) => File.ReadAllText(PathOf(relativePath));
}

public static class Expected
{
    public static string SiteAdded => Fixture.Read(Consts.Fixtures.SiteAdded);

    public static string SiteSet => Fixture.Read(Consts.Fixtures.SiteSet);

    public static string SiteSetDuplicates => Fixture.Read(Consts.Fixtures.SiteSetDuplicates);

    public static string SiteRemoved => Fixture.Read(Consts.Fixtures.SiteRemoved);

    public static string SiteBlock => Fixture.Read(Consts.Fixtures.SiteBlock);

    public static string SiteQuoted => Fixture.Read(Consts.Fixtures.SiteQuoted);

    public static string SiteTopLevel => Fixture.Read(Consts.Fixtures.SiteTopLevel);

    public static string InlineAdded => Fixture.Read(Consts.Fixtures.InlineAdded);

    public static string CrlfAdded => Fixture.Read(Consts.Fixtures.CrlfAdded);
}
